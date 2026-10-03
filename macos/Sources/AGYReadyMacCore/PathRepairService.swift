import Foundation

public struct PathRepairOutcome: Sendable {
    public let succeeded: Bool
    public let message: String
}

public struct PathRepairService {
    private struct Backup: Codable {
        let existed: Bool
        let before: String
        let after: String
    }

    public let home: URL
    private let verify: (String) -> Bool
    private var profile: URL { home.appendingPathComponent(".zprofile") }
    private var backupFile: URL { home.appendingPathComponent("Library/Application Support/AGYReadyMac/path-backup.json") }
    public var canRestore: Bool { FileManager.default.fileExists(atPath: backupFile.path) }

    public init(home: URL = FileManager.default.homeDirectoryForCurrentUser,
                verify: ((String) -> Bool)? = nil) {
        self.home = home
        self.verify = verify ?? Self.verifyInFreshShell
    }

    public func repair(agyPath: String) -> PathRepairOutcome {
        let expected = home.appendingPathComponent(".local/bin/agy").standardizedFileURL.path
        guard URL(fileURLWithPath: agyPath).standardizedFileURL.path == expected,
              FileManager.default.isExecutableFile(atPath: expected) else {
            return .init(succeeded: false, message: "只會修復目前使用者 ~/.local/bin/agy 的路徑。")
        }
        guard !isProfileSymlink else {
            return .init(succeeded: false, message: ".zprofile 是符號連結；為避免修改其他檔案，已停止。")
        }
        guard !canRestore else {
            return .init(succeeded: false, message: "已有待還原的 PATH 備份，請先還原或人工檢查。")
        }
        do {
            let existed = FileManager.default.fileExists(atPath: profile.path)
            let before = existed ? try String(contentsOf: profile, encoding: .utf8) : ""
            guard !before.contains("# >>> AGYReadyMac PATH >>>") else {
                return .init(succeeded: false, message: "已找到 AGY Ready 管理區塊，請重新檢查終端環境。")
            }
            let separator = before.isEmpty || before.hasSuffix("\n") ? "" : "\n"
            let after = before + separator + "# >>> AGYReadyMac PATH >>>\n" +
                "export PATH=\"$HOME/.local/bin:$PATH\"\n" +
                "# <<< AGYReadyMac PATH <<<\n"
            let backup = Backup(existed: existed, before: before, after: after)
            try FileManager.default.createDirectory(at: backupFile.deletingLastPathComponent(),
                                                     withIntermediateDirectories: true)
            guard !canRestore else { throw RepairError.changed }
            try write(try JSONEncoder().encode(backup), to: backupFile, permissions: 0o600)
            guard (existed ? try String(contentsOf: profile, encoding: .utf8) : "") == before else {
                try? FileManager.default.removeItem(at: backupFile)
                throw RepairError.changed
            }
            let mode = (try? FileManager.default.attributesOfItem(atPath: profile.path)[.posixPermissions] as? NSNumber)?.intValue ?? 0o600
            try write(Data(after.utf8), to: profile, permissions: mode)
            if verify(expected) {
                return .init(succeeded: true, message: "已備份 .zprofile，並在新的 zsh 程序驗證 AGY。")
            }
            if (try? String(contentsOf: profile, encoding: .utf8)) == after {
                try restoreContent(backup, permissions: mode)
                try? FileManager.default.removeItem(at: backupFile)
                return .init(succeeded: false, message: "新終端仍無法執行 AGY；本次 PATH 修改已還原。")
            }
            return .init(succeeded: false, message: "驗證失敗，且 .zprofile 已被其他程式修改；保留備份供人工檢查。")
        } catch {
            return .init(succeeded: false, message: "PATH 修復未完成：\(error.localizedDescription)")
        }
    }

    public func restore() -> PathRepairOutcome {
        guard !isProfileSymlink else {
            return .init(succeeded: false, message: ".zprofile 是符號連結，已停止還原。")
        }
        do {
            let backup = try JSONDecoder().decode(Backup.self, from: Data(contentsOf: backupFile))
            let current = try String(contentsOf: profile, encoding: .utf8)
            guard current == backup.after else {
                return .init(succeeded: false, message: ".zprofile 已被其他程式修改，為避免覆蓋已停止還原。")
            }
            let mode = (try? FileManager.default.attributesOfItem(atPath: profile.path)[.posixPermissions] as? NSNumber)?.intValue ?? 0o600
            try restoreContent(backup, permissions: mode)
            try FileManager.default.removeItem(at: backupFile)
            return .init(succeeded: true, message: "已還原原本的 .zprofile；請重新開啟終端機。")
        } catch {
            return .init(succeeded: false, message: "無法還原 PATH：\(error.localizedDescription)")
        }
    }

    private var isProfileSymlink: Bool {
        (try? FileManager.default.destinationOfSymbolicLink(atPath: profile.path)) != nil
    }

    private func restoreContent(_ backup: Backup, permissions: Int) throws {
        if backup.existed { try write(Data(backup.before.utf8), to: profile, permissions: permissions) }
        else if FileManager.default.fileExists(atPath: profile.path) { try FileManager.default.removeItem(at: profile) }
    }

    private func write(_ data: Data, to url: URL, permissions: Int) throws {
        try data.write(to: url, options: .atomic)
        try FileManager.default.setAttributes([.posixPermissions: permissions], ofItemAtPath: url.path)
    }

    private static func verifyInFreshShell(_ expected: String) -> Bool {
        let runner = CommandRunner()
        let lookup = runner.run("/bin/zsh", ["-lic", "command -v agy"], timeout: 8)
        let resolved = lookup.output.split(whereSeparator: \.isNewline).last.map(String.init) ?? ""
        guard lookup.succeeded,
              URL(fileURLWithPath: resolved).standardizedFileURL.path == expected else { return false }
        return runner.run("/bin/zsh", ["-lic", "agy --version"], timeout: 8).succeeded
    }

    private enum RepairError: Error { case changed }
}
