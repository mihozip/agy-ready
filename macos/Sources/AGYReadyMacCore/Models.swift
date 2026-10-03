import Foundation

public enum CheckStatus: String, Sendable {
    case pass = "通過"
    case warning = "注意"
    case fail = "失敗"
}

public struct CheckItem: Identifiable, Sendable {
    public let id: String
    public let status: CheckStatus
    public let title: String
    public let detail: String

    public init(_ id: String, _ status: CheckStatus, _ title: String, _ detail: String = "") {
        self.id = id
        self.status = status
        self.title = title
        self.detail = detail
    }
}

public struct DiagnosticSnapshot: Sendable {
    public let checks: [CheckItem]
    public let agyWorking: Bool
    public let agyCandidate: String?
    public let agyNeedsPathRepair: Bool
    public let nodeSupported: Bool
    public let npmAvailable: Bool
    public let claspWorking: Bool
    public let npmPath: String?
    public let shellPath: String

    public init(checks: [CheckItem], agyWorking: Bool, agyCandidate: String?,
                agyNeedsPathRepair: Bool, nodeSupported: Bool, npmAvailable: Bool,
                claspWorking: Bool, npmPath: String?, shellPath: String) {
        self.checks = checks
        self.agyWorking = agyWorking
        self.agyCandidate = agyCandidate
        self.agyNeedsPathRepair = agyNeedsPathRepair
        self.nodeSupported = nodeSupported
        self.npmAvailable = npmAvailable
        self.claspWorking = claspWorking
        self.npmPath = npmPath
        self.shellPath = shellPath
    }
}

public struct CommandResult: Sendable {
    public let exitCode: Int32?
    public let output: String
    public let timedOut: Bool
    public var succeeded: Bool { exitCode == 0 && !timedOut }
}
