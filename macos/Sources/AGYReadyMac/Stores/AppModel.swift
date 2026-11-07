import Foundation
import AGYReadyMacCore

@MainActor
final class AppModel {
    var onChange: (() -> Void)?
    private(set) var snapshot: DiagnosticSnapshot? { didSet { onChange?() } }
    private(set) var isBusy = false { didSet { onChange?() } }
    private(set) var activity = "就緒" { didSet { onChange?() } }
    private(set) var message = "開啟後會檢查本機環境。" { didSet { onChange?() } }
    private(set) var progress: Double? { didSet { onChange?() } }
    private(set) var lastError: String? { didSet { onChange?() } }

    var canRestorePath: Bool { PathRepairService().canRestore }

    func setSetupMessage(_ text: String) { message = text }

    func refresh() async {
        guard !isBusy else { return }
        isBusy = true
        activity = "正在檢查環境"
        progress = nil
        let result = await Task.detached { DiagnosticService().run() }.value
        snapshot = result
        message = "檢查完成。"
        activity = "就緒"
        isBusy = false
    }

    func installAGY() async -> Bool {
        guard !isBusy else { return false }
        isBusy = true
        activity = "正在下載 AGY 官方啟動腳本"
        progress = nil
        do {
            try await InstallationService().installAGY { fraction in
                await MainActor.run {
                    self.progress = fraction
                    self.activity = fraction == nil ? "正在安裝 AGY CLI" : "正在下載 AGY 官方啟動腳本"
                }
            }
            activity = "正在驗證 AGY"
            snapshot = await Task.detached { DiagnosticService().run() }.value
            message = snapshot?.agyWorking == true
                ? "AGY 已安裝並通過版本驗證。" : "安裝程序已結束，但 AGY 尚未通過驗證；請查看檢查項目。"
            lastError = snapshot?.agyWorking == true ? nil : message
        } catch {
            message = error.localizedDescription
            lastError = message
            snapshot = await Task.detached { DiagnosticService().run() }.value
        }
        progress = nil
        activity = "就緒"
        isBusy = false
        return snapshot?.agyWorking == true && lastError == nil
    }

    func installClasp() async {
        guard !isBusy, let snapshot, snapshot.nodeSupported, snapshot.npmAvailable,
              let npmPath = snapshot.npmPath else { return }
        isBusy = true
        activity = "正在安裝 clasp"
        progress = nil
        do {
            try await InstallationService().installClasp(npmPath: npmPath,
                                                         shellPath: snapshot.shellPath)
            self.snapshot = await Task.detached { DiagnosticService().run() }.value
            message = self.snapshot?.claspWorking == true
                ? "clasp 已安裝並通過版本驗證。" : "npm 已結束，但 clasp 尚未通過驗證。"
            lastError = self.snapshot?.claspWorking == true ? nil : message
        } catch {
            message = error.localizedDescription
            lastError = message
            self.snapshot = await Task.detached { DiagnosticService().run() }.value
        }
        activity = "就緒"
        isBusy = false
    }

    func repairPath() async {
        guard !isBusy, let candidate = snapshot?.agyCandidate else { return }
        isBusy = true
        activity = "正在修復 zsh PATH"
        progress = nil
        let outcome = await Task.detached { PathRepairService().repair(agyPath: candidate) }.value
        message = outcome.message
        lastError = outcome.succeeded ? nil : message
        snapshot = await Task.detached { DiagnosticService().run() }.value
        activity = "就緒"
        isBusy = false
    }

    func restorePath() async {
        guard !isBusy else { return }
        isBusy = true
        activity = "正在還原 zsh PATH"
        progress = nil
        let outcome = await Task.detached { PathRepairService().restore() }.value
        message = outcome.message
        lastError = outcome.succeeded ? nil : message
        snapshot = await Task.detached { DiagnosticService().run() }.value
        activity = "就緒"
        isBusy = false
    }
}
