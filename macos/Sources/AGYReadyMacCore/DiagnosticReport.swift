import Foundation

public struct DiagnosticReport {
    public static func render(_ snapshot: DiagnosticSnapshot, appVersion: String,
                              lastError: String? = nil, home: URL = FileManager.default.homeDirectoryForCurrentUser,
                              date: Date = Date()) -> String {
        let stamp = ISO8601DateFormatter().string(from: date)
        var lines = [
            "AGY Ready Diagnostic Report",
            "App Version: \(safe(appVersion, home: home))",
            "Timestamp: \(stamp)",
            "",
            "CHECKS"
        ]
        for check in snapshot.checks {
            lines.append("[\(check.status.rawValue)] \(check.id): \(safe(check.title, home: home))")
            if !check.detail.isEmpty {
                lines.append("  \(safe(check.detail, home: home))")
            }
        }
        lines += [
            "",
            "RESULT",
            "AGY: \(snapshot.agyWorking ? "可使用" : snapshot.agyNeedsPathRepair ? "需要修復 PATH" : "尚未可使用")",
            "Node.js 20+: \(snapshot.nodeSupported ? "可使用" : "尚未可使用")",
            "npm: \(snapshot.npmAvailable ? "可使用" : "尚未可使用")",
            "clasp: \(snapshot.claspWorking ? "可使用" : "尚未可使用")",
            "",
            "LAST ERROR",
            lastError.map { safe($0, home: home) } ?? "無"
        ]
        return lines.joined(separator: "\n") + "\n"
    }

    private static func safe(_ value: String, home: URL) -> String {
        let homePath = home.standardizedFileURL.path
        let replacedHome = value.replacingOccurrences(of: homePath + "/", with: "$HOME/")
            .replacingOccurrences(of: homePath + ":", with: "$HOME:")
        let pattern = #"/(?:Users|home)/[^/\s:]+"#
        let redacted = replacedHome.replacingOccurrences(of: pattern, with: "$HOME", options: .regularExpression)
        return redacted.unicodeScalars.map { scalar in
            CharacterSet.controlCharacters.contains(scalar) && scalar != "\t" ? " " : String(scalar)
        }.joined()
    }
}
