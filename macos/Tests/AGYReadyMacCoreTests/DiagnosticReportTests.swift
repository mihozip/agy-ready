import Foundation
import XCTest
@testable import AGYReadyMacCore

final class DiagnosticReportTests: XCTestCase {
    func testReportIncludesChecksAndRedactsHomeAndLastError() {
        let home = URL(fileURLWithPath: "/Users/Example Teacher")
        let snapshot = DiagnosticSnapshot(
            checks: [CheckItem("AGY-001", .warning, "已找到 AGY",
                               "/Users/Example Teacher/.local/bin/agy")],
            agyWorking: false, agyCandidate: home.appendingPathComponent(".local/bin/agy").path,
            agyNeedsPathRepair: true, nodeSupported: false, npmAvailable: false,
            claspWorking: false, npmPath: nil, shellPath: "/usr/bin")

        let report = DiagnosticReport.render(snapshot, appVersion: "0.1.0",
            lastError: "無法執行 /Users/Example Teacher/.local/bin/agy",
            home: home, date: Date(timeIntervalSince1970: 0))

        XCTAssertTrue(report.contains("AGY-001"))
        XCTAssertTrue(report.contains("App Version: 0.1.0"))
        XCTAssertTrue(report.contains("Timestamp: 1970-01-01T00:00:00Z"))
        XCTAssertTrue(report.contains("$HOME/.local/bin/agy"))
        XCTAssertTrue(report.contains("LAST ERROR"))
        XCTAssertFalse(report.contains("Example Teacher"))
    }

    func testSimilarHomePrefixDoesNotAlterAnotherAccount() {
        let home = URL(fileURLWithPath: "/Users/al")
        let snapshot = DiagnosticSnapshot(
            checks: [CheckItem("AGY-001", .warning, "check", "/Users/albert/bin/agy")],
            agyWorking: false, agyCandidate: nil, agyNeedsPathRepair: false,
            nodeSupported: false, npmAvailable: false, claspWorking: false,
            npmPath: nil, shellPath: "/usr/bin")
        let report = DiagnosticReport.render(snapshot, appVersion: "1", home: home)
        XCTAssertTrue(report.contains("$HOME/bin/agy"))
        XCTAssertFalse(report.contains("albert"))
    }
}
