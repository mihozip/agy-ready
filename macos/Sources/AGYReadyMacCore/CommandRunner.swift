import Foundation

public struct CommandRunner: Sendable {
    public init() {}

    public func run(_ executable: String, _ arguments: [String],
                    environment: [String: String]? = nil, timeout: TimeInterval = 10,
                    captureOutput: Bool = true) -> CommandResult {
        let process = Process()
        process.executableURL = URL(fileURLWithPath: executable)
        process.arguments = arguments
        if let environment { process.environment = ProcessInfo.processInfo.environment.merging(environment) { _, new in new } }
        let output = Pipe()
        process.standardOutput = captureOutput ? output : FileHandle.nullDevice
        process.standardError = captureOutput ? output : FileHandle.nullDevice
        let timeoutState = TimeoutState()
        do {
            try process.run()
        } catch {
            return CommandResult(exitCode: nil, output: error.localizedDescription, timedOut: false)
        }
        DispatchQueue.global().asyncAfter(deadline: .now() + timeout) {
            if process.isRunning {
                timeoutState.markTimedOut()
                process.terminate()
            }
        }
        let data = captureOutput ? output.fileHandleForReading.readDataToEndOfFile() : Data()
        process.waitUntilExit()
        return CommandResult(exitCode: process.terminationStatus,
                             output: String(decoding: data.prefix(16_384), as: UTF8.self),
                             timedOut: timeoutState.timedOut)
    }
}

private final class TimeoutState: @unchecked Sendable {
    private let lock = NSLock()
    private var value = false
    func markTimedOut() { lock.lock(); value = true; lock.unlock() }
    var timedOut: Bool { lock.lock(); defer { lock.unlock() }; return value }
}
