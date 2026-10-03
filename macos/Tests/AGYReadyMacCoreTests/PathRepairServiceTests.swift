import Foundation
import XCTest
@testable import AGYReadyMacCore

final class PathRepairServiceTests: XCTestCase {
    private var home: URL!

    override func setUpWithError() throws {
        home = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        try FileManager.default.createDirectory(at: home.appendingPathComponent(".local/bin"),
                                                withIntermediateDirectories: true)
        let agy = home.appendingPathComponent(".local/bin/agy")
        try Data("#!/bin/sh\necho 1.0\n".utf8).write(to: agy)
        try FileManager.default.setAttributes([.posixPermissions: 0o700], ofItemAtPath: agy.path)
    }

    override func tearDownWithError() throws {
        try FileManager.default.removeItem(at: home)
    }

    func testRepairAndRestoreNewProfile() throws {
        let service = PathRepairService(home: home, verify: { _ in true })
        let result = service.repair(agyPath: home.appendingPathComponent(".local/bin/agy").path)
        XCTAssertTrue(result.succeeded)
        XCTAssertTrue(service.canRestore)
        XCTAssertTrue(try String(contentsOf: home.appendingPathComponent(".zprofile"), encoding: .utf8)
            .contains("export PATH=\"$HOME/.local/bin:$PATH\""))

        XCTAssertTrue(service.restore().succeeded)
        XCTAssertFalse(service.canRestore)
        XCTAssertFalse(FileManager.default.fileExists(atPath: home.appendingPathComponent(".zprofile").path))
    }

    func testFailedVerificationRollsBack() {
        let service = PathRepairService(home: home, verify: { _ in false })
        let result = service.repair(agyPath: home.appendingPathComponent(".local/bin/agy").path)
        XCTAssertFalse(result.succeeded)
        XCTAssertFalse(service.canRestore)
        XCTAssertFalse(FileManager.default.fileExists(atPath: home.appendingPathComponent(".zprofile").path))
    }

    func testRestoreRefusesExternalChange() throws {
        let profile = home.appendingPathComponent(".zprofile")
        try Data("# existing\n".utf8).write(to: profile)
        let service = PathRepairService(home: home, verify: { _ in true })
        XCTAssertTrue(service.repair(agyPath: home.appendingPathComponent(".local/bin/agy").path).succeeded)
        try Data("# changed by user\n".utf8).write(to: profile)

        XCTAssertFalse(service.restore().succeeded)
        XCTAssertTrue(service.canRestore)
        XCTAssertEqual(try String(contentsOf: profile, encoding: .utf8), "# changed by user\n")
    }
}
