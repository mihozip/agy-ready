import Foundation

public struct DiagnosticService: Sendable {
    private let runner = CommandRunner()
    public let home: URL

    public init(home: URL = FileManager.default.homeDirectoryForCurrentUser) {
        self.home = home
    }

    public func run() -> DiagnosticSnapshot {
        let shellPath = readShellPath()
        let environment = ["PATH": shellPath]
        let agy = resolve("agy", in: shellPath)
        let candidate = home.appendingPathComponent(".local/bin/agy").path
        let candidateExists = FileManager.default.isExecutableFile(atPath: candidate)
        let agyResult = agy.map { runner.run($0, ["--version"], environment: environment) }
        let agyWorking = agyResult?.succeeded == true && !versionText(agyResult!.output).isEmpty
        let git = resolve("git", in: shellPath)
        let gitResult = git.map { runner.run($0, ["--version"], environment: environment) }
        let node = resolve("node", in: shellPath)
        let nodeResult = node.map { runner.run($0, ["--version"], environment: environment) }
        let nodeVersion = nodeResult?.succeeded == true ? versionText(nodeResult!.output) : ""
        let nodeMajor = Int(nodeVersion.trimmingCharacters(in: CharacterSet(charactersIn: "vV"))
            .split(separator: ".").first ?? "")
        let nodeSupported = nodeMajor.map { $0 >= 20 } ?? false
        let npm = resolve("npm", in: shellPath)
        let npmResult = npm.map { runner.run($0, ["--version"], environment: environment) }
        let npmAvailable = npmResult?.succeeded == true
        let clasp = resolve("clasp", in: shellPath)
        let claspResult = clasp.map { runner.run($0, ["--version"], environment: environment) }
        let claspWorking = claspResult?.succeeded == true && !versionText(claspResult!.output).isEmpty
        let arch = runner.run("/usr/bin/uname", ["-m"]).output.trimmingCharacters(in: .whitespacesAndNewlines)

        let checks: [CheckItem] = [
            CheckItem("SYS-001", .pass, "macOS \(ProcessInfo.processInfo.operatingSystemVersionString)"),
            CheckItem("SYS-002", arch == "arm64" || arch == "x86_64" ? .pass : .warning,
                      "處理器架構：\(arch.isEmpty ? "未知" : arch)"),
            CheckItem("SYS-003", .pass, "zsh 可使用"),
            CheckItem("AGY-001", agyWorking ? .pass : candidateExists ? .warning : .fail,
                      agyWorking ? "AGY CLI \(versionText(agyResult!.output)) 可使用" :
                        candidateExists ? "已找到 AGY，但目前終端 PATH 無法正常執行" : "尚未找到 AGY CLI",
                      agy ?? (candidateExists ? candidate : "")),
            CheckItem("PATH-001", agy != nil ? .pass : candidateExists ? .warning : .fail,
                      agy != nil ? "終端 PATH 已包含 AGY" : candidateExists ? "需要修復 zsh PATH" : "尚無 AGY 路徑"),
            CheckItem("DEV-001", gitResult?.succeeded == true ? .pass : .warning,
                      gitResult?.succeeded == true ? "Git 可使用" : "Git 尚未可用"),
            CheckItem("DEV-002", nodeSupported ? .pass : .fail,
                      nodeSupported ? "Node.js \(nodeVersion) 可使用" : "需要 Node.js 20 以上"),
            CheckItem("DEV-003", npmAvailable ? .pass : .fail,
                      npmAvailable ? "npm 可使用" : "找不到 npm 或無法執行"),
            CheckItem("DEV-004", claspWorking ? .pass : .fail,
                      claspWorking ? "clasp \(versionText(claspResult!.output)) 可使用" :
                        nodeSupported && npmAvailable ? "可安裝 clasp" : "需先備妥 Node.js 與 npm")
        ]
        return DiagnosticSnapshot(checks: checks, agyWorking: agyWorking,
                                  agyCandidate: candidateExists ? candidate : nil,
                                  agyNeedsPathRepair: candidateExists && !agyWorking,
                                  nodeSupported: nodeSupported, npmAvailable: npmAvailable,
                                  claspWorking: claspWorking, npmPath: npm, shellPath: shellPath)
    }

    public func readShellPath() -> String {
        let result = runner.run("/bin/zsh", ["-lic", "print -r -- AGYREADY_PATH=$PATH"], timeout: 8)
        let line = result.output.split(separator: "\n").last { $0.hasPrefix("AGYREADY_PATH=") }
        return line.map { String($0.dropFirst("AGYREADY_PATH=".count)) }
            ?? ProcessInfo.processInfo.environment["PATH"] ?? "/usr/bin:/bin:/usr/sbin:/sbin"
    }

    public func resolve(_ command: String, in path: String) -> String? {
        guard !command.contains("/") else { return nil }
        for directory in path.split(separator: ":") {
            let candidate = URL(fileURLWithPath: String(directory)).appendingPathComponent(command).path
            if FileManager.default.isExecutableFile(atPath: candidate) { return candidate }
        }
        return nil
    }

    private func versionText(_ output: String) -> String {
        String(output.split(whereSeparator: \.isNewline).first ?? "")
            .trimmingCharacters(in: .whitespacesAndNewlines).prefix(100).description
    }
}
