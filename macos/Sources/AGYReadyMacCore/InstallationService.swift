import Foundation

public enum InstallationError: LocalizedError {
    case badResponse
    case oversizedScript
    case failed(String)

    public var errorDescription: String? {
        switch self {
        case .badResponse: "官方下載來源無法確認。"
        case .oversizedScript: "官方啟動腳本超過允許大小，已停止安裝。"
        case .failed(let message): message
        }
    }
}

public struct InstallationService: Sendable {
    private static let scriptURL = URL(string: "https://antigravity.google/cli/install.sh")!
    private static let maxScriptBytes = 1_000_000
    private let runner = CommandRunner()

    public init() {}

    public func installAGY(progress: @escaping @Sendable (Double?) async -> Void) async throws {
        let (bytes, response) = try await URLSession.shared.bytes(from: Self.scriptURL)
        guard let http = response as? HTTPURLResponse, http.statusCode == 200,
              http.url?.scheme == "https", http.url?.host == "antigravity.google" else {
            throw InstallationError.badResponse
        }
        let expected = response.expectedContentLength
        guard expected <= Self.maxScriptBytes else { throw InstallationError.oversizedScript }
        var data = Data()
        var reported = -1
        await progress(expected > 0 ? 0 : nil)
        for try await byte in bytes {
            data.append(byte)
            if data.count > Self.maxScriptBytes { throw InstallationError.oversizedScript }
            if expected > 0 {
                let percent = min(100, data.count * 100 / Int(expected))
                if percent != reported {
                    reported = percent
                    await progress(Double(percent) / 100)
                }
            }
        }
        guard expected < 0 || data.count == expected else {
            throw InstallationError.failed("啟動腳本下載不完整。")
        }
        let script = FileManager.default.temporaryDirectory
            .appendingPathComponent("AGYReadyMac-\(UUID().uuidString).sh")
        try data.write(to: script, options: .atomic)
        defer { try? FileManager.default.removeItem(at: script) }
        await progress(nil)
        let scriptPath = script.path
        let result = await Task.detached {
            runner.run("/bin/bash", [scriptPath], timeout: 300, captureOutput: false)
        }.value
        guard result.succeeded else {
            throw InstallationError.failed(result.timedOut ? "AGY 官方安裝程序逾時。" :
                "AGY 官方安裝程序未成功（Exit Code \(result.exitCode ?? -1)）。")
        }
    }

    public func installClasp(npmPath: String, shellPath: String) async throws {
        let result = await Task.detached {
            runner.run(npmPath, ["install", "@google/clasp", "-g"],
                       environment: ["PATH": shellPath], timeout: 300, captureOutput: false)
        }.value
        guard result.succeeded else {
            throw InstallationError.failed(result.timedOut ? "clasp 安裝逾時。" :
                "npm 安裝 clasp 未成功（Exit Code \(result.exitCode ?? -1)）。")
        }
    }
}
