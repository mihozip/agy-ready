import AppKit
import AGYReadyMacCore

@main
@MainActor
enum Main {
    private static let delegate = AppDelegate()

    static func main() {
        let app = NSApplication.shared
        app.delegate = delegate
        app.setActivationPolicy(.regular)
        app.run()
    }
}

@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate {
    private let model = AppModel()
    private var window: NSWindow!
    private let tabs = NSTabView()
    private let overviewRows = NSStackView()
    private let agyStatus = NSTextField(labelWithString: "尚未檢查")
    private let toolsStatus = NSTextField(labelWithString: "尚未檢查")
    private let activityLabel = NSTextField(labelWithString: "就緒")
    private let messageLabel = NSTextField(labelWithString: "")
    private let progress = NSProgressIndicator()
    private let agyButton = NSButton(title: "安裝 AGY CLI", target: nil, action: nil)
    private let agySetupButton = NSButton(title: "開啟 AGY 登入與色彩設定", target: nil, action: nil)
    private let repairButton = NSButton(title: "修復 zsh PATH", target: nil, action: nil)
    private let restoreButton = NSButton(title: "還原本次 PATH", target: nil, action: nil)
    private let nodeButton = NSButton(title: "下載 Node.js + npm", target: nil, action: nil)
    private let claspButton = NSButton(title: "安裝 clasp", target: nil, action: nil)
    private let copyReportButton = NSButton(title: "複製診斷報告", target: nil, action: nil)
    private let saveReportButton = NSButton(title: "儲存報告", target: nil, action: nil)

    func applicationDidFinishLaunching(_ notification: Notification) {
        if let iconURL = Bundle.main.url(forResource: "AGYReadyMac", withExtension: "icns"),
           let icon = NSImage(contentsOf: iconURL) { NSApp.applicationIconImage = icon }
        buildMenu()
        buildWindow()
        model.onChange = { [weak self] in self?.render() }
        render()
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
        Task { await model.refresh() }
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { true }

    private func buildWindow() {
        window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 920, height: 640),
                          styleMask: [.titled, .closable, .miniaturizable, .resizable],
                          backing: .buffered, defer: false)
        window.title = "AGY CLI-CLASP 安裝小幫手（標標版）"
        window.minSize = NSSize(width: 780, height: 540)
        window.center()

        let root = NSStackView()
        root.orientation = .vertical
        root.alignment = .leading
        root.spacing = 0
        root.translatesAutoresizingMaskIntoConstraints = false
        window.contentView?.addSubview(root)
        guard let content = window.contentView else { return }
        NSLayoutConstraint.activate([
            root.leadingAnchor.constraint(equalTo: content.leadingAnchor),
            root.trailingAnchor.constraint(equalTo: content.trailingAnchor),
            root.topAnchor.constraint(equalTo: content.topAnchor),
            root.bottomAnchor.constraint(equalTo: content.bottomAnchor)
        ])

        let header = makeHeader()
        root.addArrangedSubview(header)
        header.widthAnchor.constraint(equalTo: root.widthAnchor).isActive = true
        tabs.translatesAutoresizingMaskIntoConstraints = false
        tabs.tabViewType = .topTabsBezelBorder
        tabs.addTabViewItem(makeTab("環境健檢", content: makeOverview()))
        tabs.addTabViewItem(makeTab("AGY CLI", content: makeAGYPage()))
        tabs.addTabViewItem(makeTab("Node.js / clasp", content: makeToolsPage()))
        tabs.addTabViewItem(makeTab("作者", content: makeAuthorPage()))
        root.addArrangedSubview(tabs)
        tabs.widthAnchor.constraint(equalTo: root.widthAnchor).isActive = true
        tabs.setContentCompressionResistancePriority(.defaultLow, for: .vertical)
        let status = makeStatusBar()
        root.addArrangedSubview(status)
        status.widthAnchor.constraint(equalTo: root.widthAnchor).isActive = true
    }

    private func makeHeader() -> NSView {
        let title = label("AGY CLI-CLASP 安裝小幫手（標標版）", size: 21, bold: true)
        let subtitle = label("macOS 環境健檢與安裝引導", size: 12, color: .secondaryLabelColor)
        let titles = vertical([title, subtitle], spacing: 3)
        let icon = NSImageView(image: NSApp.applicationIconImage)
        icon.imageScaling = .scaleProportionallyUpOrDown
        icon.translatesAutoresizingMaskIntoConstraints = false
        NSLayoutConstraint.activate([
            icon.widthAnchor.constraint(equalToConstant: 48),
            icon.heightAnchor.constraint(equalToConstant: 48)
        ])
        let refresh = button("重新檢查", action: #selector(refreshClicked))
        let spacer = NSView()
        let row = horizontal([icon, titles, spacer, refresh], spacing: 14)
        row.edgeInsets = NSEdgeInsets(top: 14, left: 24, bottom: 14, right: 24)
        return row
    }

    private func makeOverview() -> NSView {
        overviewRows.orientation = .vertical
        overviewRows.alignment = .leading
        overviewRows.spacing = 7
        configure(copyReportButton, action: #selector(copyReportClicked))
        configure(saveReportButton, action: #selector(saveReportClicked))
        return page([label("環境檢查", size: 18, bold: true),
                     horizontal([copyReportButton, saveReportButton], spacing: 10), overviewRows])
    }

    private func makeAGYPage() -> NSView {
        agyStatus.font = .systemFont(ofSize: 14)
        configure(agyButton, action: #selector(installAGYClicked))
        configure(agySetupButton, action: #selector(agySetupClicked))
        configure(repairButton, action: #selector(repairClicked))
        configure(restoreButton, action: #selector(restoreClicked))
        let actions = horizontal([agyButton, repairButton, restoreButton], spacing: 10)
        return page([
            label("AGY CLI", size: 18, bold: true),
            agyStatus,
            actions,
            agySetupButton,
            label("安裝成功後會開啟 AGY 首次設定，請在官方視窗完成登入、授權碼與色彩選擇。", size: 12, color: .secondaryLabelColor),
            label("PATH 修復只管理目前使用者的 ~/.zprofile，並保留可還原備份。", size: 12, color: .secondaryLabelColor)
        ])
    }

    private func makeToolsPage() -> NSView {
        toolsStatus.font = .systemFont(ofSize: 14)
        configure(nodeButton, action: #selector(nodeClicked))
        configure(claspButton, action: #selector(claspClicked))
        let actions = horizontal([nodeButton, claspButton], spacing: 10)
        return page([
            label("Node.js / npm / clasp", size: 18, bold: true),
            toolsStatus,
            actions,
            label("clasp 需要 Node.js 20 以上與 npm。安裝 Node.js 後請按「重新檢查」。", size: 12, color: .secondaryLabelColor)
        ])
    }

    private func makeAuthorPage() -> NSView {
        let facebook = button("facebook.com/albert.peng.56", action: #selector(facebookClicked))
        facebook.isBordered = false
        facebook.contentTintColor = .linkColor
        let email = button("mihozip@gmail.com", action: #selector(emailClicked))
        email.isBordered = false
        email.contentTintColor = .linkColor
        return page([
            label("作者", size: 18, bold: true),
            label("苗栗縣永貞國民小學彭智標老師", size: 16),
            label("Facebook", size: 13, bold: true),
            facebook,
            label("聯絡信箱", size: 13, bold: true),
            email
        ])
    }

    private func makeStatusBar() -> NSView {
        activityLabel.font = .systemFont(ofSize: 12, weight: .semibold)
        messageLabel.font = .systemFont(ofSize: 12)
        messageLabel.textColor = .secondaryLabelColor
        messageLabel.lineBreakMode = .byTruncatingTail
        progress.controlSize = .small
        progress.translatesAutoresizingMaskIntoConstraints = false
        progress.widthAnchor.constraint(equalToConstant: 130).isActive = true
        let spacer = NSView()
        let row = horizontal([activityLabel, progress, spacer, messageLabel], spacing: 12)
        row.edgeInsets = NSEdgeInsets(top: 10, left: 22, bottom: 10, right: 22)
        return row
    }

    private func render() {
        guard window != nil else { return }
        activityLabel.stringValue = model.activity
        messageLabel.stringValue = model.message
        progress.isHidden = !model.isBusy
        if model.isBusy {
            if let fraction = model.progress {
                progress.stopAnimation(nil)
                progress.isIndeterminate = false
                progress.doubleValue = fraction * 100
            } else {
                progress.isIndeterminate = true
                progress.startAnimation(nil)
            }
        } else { progress.stopAnimation(nil) }

        for view in overviewRows.arrangedSubviews { overviewRows.removeArrangedSubview(view); view.removeFromSuperview() }
        if let snapshot = model.snapshot {
            for check in snapshot.checks {
                let symbol = check.status == .pass ? "✓" : check.status == .warning ? "!" : "×"
                let color: NSColor = check.status == .pass ? .systemGreen : check.status == .warning ? .systemOrange : .systemRed
                let marker = label(symbol, size: 14, bold: true, color: color)
                marker.alignment = .center
                marker.widthAnchor.constraint(equalToConstant: 22).isActive = true
                let code = label(check.id, size: 12, color: .secondaryLabelColor)
                code.widthAnchor.constraint(equalToConstant: 75).isActive = true
                let summary = label(check.title, size: 13)
                summary.toolTip = check.detail.isEmpty ? check.title : check.detail
                overviewRows.addArrangedSubview(horizontal([marker, code, summary], spacing: 9))
            }
            agyStatus.stringValue = snapshot.agyWorking ? "AGY CLI 已可使用。" :
                snapshot.agyCandidate != nil ? "已找到 AGY，請檢查終端 PATH。" : "尚未安裝 AGY CLI。"
            toolsStatus.stringValue = "Node.js \(snapshot.nodeSupported ? "就緒" : "需安裝") · npm \(snapshot.npmAvailable ? "就緒" : "需修復") · clasp \(snapshot.claspWorking ? "就緒" : "未安裝")"
            agyButton.isEnabled = !model.isBusy && !snapshot.agyWorking
            agySetupButton.isEnabled = !model.isBusy && snapshot.agyWorking
            repairButton.isEnabled = !model.isBusy && snapshot.agyNeedsPathRepair
            nodeButton.isEnabled = !model.isBusy && !(snapshot.nodeSupported && snapshot.npmAvailable)
            claspButton.isEnabled = !model.isBusy && snapshot.nodeSupported && snapshot.npmAvailable && !snapshot.claspWorking
        } else {
            overviewRows.addArrangedSubview(label("正在檢查…", size: 13))
            agyButton.isEnabled = false
            agySetupButton.isEnabled = false
            repairButton.isEnabled = false
            nodeButton.isEnabled = false
            claspButton.isEnabled = false
        }
        restoreButton.isEnabled = !model.isBusy && model.canRestorePath
        copyReportButton.isEnabled = !model.isBusy && model.snapshot != nil
        saveReportButton.isEnabled = !model.isBusy && model.snapshot != nil
    }

    private func buildMenu() {
        let main = NSMenu()
        let appItem = NSMenuItem()
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "結束 AGY 安裝小幫手", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appItem.submenu = appMenu
        main.addItem(appItem)
        let toolItem = NSMenuItem()
        let toolMenu = NSMenu(title: "工具")
        toolMenu.addItem(withTitle: "重新檢查", action: #selector(refreshClicked), keyEquivalent: "r")
        toolItem.submenu = toolMenu
        main.addItem(toolItem)
        NSApp.mainMenu = main
    }

    @objc private func refreshClicked() { Task { await model.refresh() } }
    @objc private func installAGYClicked() {
        if confirm("安裝 AGY CLI", "將從 Google 官方網站下載並執行 macOS 安裝腳本。驗證成功後會開啟 AGY 登入與色彩設定。") {
            Task {
                if await model.installAGY() { openAgySetup() }
            }
        }
    }
    @objc private func agySetupClicked() { openAgySetup() }

    private func openAgySetup() {
        guard !model.isBusy, let snapshot = model.snapshot, snapshot.agyWorking,
              let executable = DiagnosticService().resolve("agy", in: snapshot.shellPath) else { return }
        do {
            let workspace = FileManager.default.homeDirectoryForCurrentUser
                .appendingPathComponent("Library/Application Support/AGYReadyMac/Onboarding")
            try FileManager.default.createDirectory(at: workspace, withIntermediateDirectories: true)
            let command = "cd \(shellQuote(workspace.path)) && \(shellQuote(executable))"
            let script = "tell application \"Terminal\" to do script \"\(appleScriptEscape(command))\""
            let process = Process()
            process.executableURL = URL(fileURLWithPath: "/usr/bin/osascript")
            process.arguments = ["-e", script]
            process.terminationHandler = { [weak self] process in
                guard process.terminationStatus != 0 else { return }
                DispatchQueue.main.async {
                    self?.model.setSetupMessage("無法開啟 AGY 設定視窗；請在終端機執行 agy 完成首次設定。")
                }
            }
            try process.run()
            model.setSetupMessage("已開啟 AGY 首次設定。請在 Terminal 完成登入、授權碼與色彩選擇；工作資料夾信任會依資料夾個別詢問。")
        } catch {
            model.setSetupMessage("無法開啟 AGY 設定視窗；請在終端機執行 agy 完成首次設定。")
        }
    }

    private func shellQuote(_ value: String) -> String {
        "'" + value.replacingOccurrences(of: "'", with: "'\\''") + "'"
    }

    private func appleScriptEscape(_ value: String) -> String {
        value.replacingOccurrences(of: "\\", with: "\\\\")
            .replacingOccurrences(of: "\"", with: "\\\"")
    }
    @objc private func repairClicked() {
        if confirm("修復 zsh PATH", "將備份 ~/.zprofile，加入 ~/.local/bin，並在新的 zsh 程序驗證 AGY。") {
            Task { await model.repairPath() }
        }
    }
    @objc private func restoreClicked() {
        if confirm("還原 zsh PATH", "僅在 ~/.zprofile 未被其他程式更動時還原備份。") {
            Task { await model.restorePath() }
        }
    }
    @objc private func nodeClicked() { open("https://nodejs.org/en/download") }
    @objc private func claspClicked() {
        if confirm("安裝 clasp", "將執行 npm install @google/clasp -g，完成後重新驗證。") {
            Task { await model.installClasp() }
        }
    }
    @objc private func facebookClicked() { open("https://www.facebook.com/albert.peng.56?locale=zh_TW") }
    @objc private func emailClicked() { open("mailto:mihozip@gmail.com") }

    private func reportText() -> String? {
        guard let snapshot = model.snapshot else { return nil }
        let version = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "未知"
        return DiagnosticReport.render(snapshot, appVersion: version, lastError: model.lastError)
    }

    @objc private func copyReportClicked() {
        guard let report = reportText() else { return }
        NSPasteboard.general.clearContents()
        let copied = NSPasteboard.general.setString(report, forType: .string)
        showReportResult(copied ? "診斷報告已複製到剪貼簿。" : "無法複製診斷報告。")
    }

    @objc private func saveReportClicked() {
        guard let report = reportText() else { return }
        let panel = NSSavePanel()
        panel.nameFieldStringValue = "AGYReady-Diagnostic-Report.txt"
        panel.allowedContentTypes = [.plainText]
        guard panel.runModal() == .OK, let url = panel.url else { return }
        do {
            try report.write(to: url, atomically: true, encoding: .utf8)
            showReportResult("診斷報告已儲存。")
        } catch {
            showReportResult("無法儲存報告：\(error.localizedDescription)")
        }
    }

    private func showReportResult(_ message: String) {
        let alert = NSAlert()
        alert.messageText = message
        alert.runModal()
    }

    private func confirm(_ title: String, _ detail: String) -> Bool {
        let alert = NSAlert()
        alert.messageText = title
        alert.informativeText = detail
        alert.addButton(withTitle: "繼續")
        alert.addButton(withTitle: "取消")
        return alert.runModal() == .alertFirstButtonReturn
    }

    private func open(_ address: String) {
        guard let url = URL(string: address), NSWorkspace.shared.open(url) else {
            model.onChange?()
            let alert = NSAlert()
            alert.messageText = "無法開啟連結"
            alert.informativeText = address
            alert.runModal()
            return
        }
        _ = url
    }

    private func makeTab(_ title: String, content: NSView) -> NSTabViewItem {
        let item = NSTabViewItem(identifier: title)
        item.label = title
        item.view = content
        return item
    }

    private func page(_ views: [NSView]) -> NSView {
        let container = NSView()
        let stack = vertical(views, spacing: 16)
        stack.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(stack)
        NSLayoutConstraint.activate([
            stack.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 28),
            stack.trailingAnchor.constraint(lessThanOrEqualTo: container.trailingAnchor, constant: -20),
            stack.topAnchor.constraint(equalTo: container.topAnchor, constant: 28)
        ])
        return container
    }

    private func vertical(_ views: [NSView], spacing: CGFloat) -> NSStackView {
        let stack = NSStackView(views: views)
        stack.orientation = .vertical
        stack.alignment = .leading
        stack.spacing = spacing
        return stack
    }

    private func horizontal(_ views: [NSView], spacing: CGFloat) -> NSStackView {
        let stack = NSStackView(views: views)
        stack.orientation = .horizontal
        stack.alignment = .centerY
        stack.spacing = spacing
        return stack
    }

    private func label(_ text: String, size: CGFloat, bold: Bool = false,
                       color: NSColor = .labelColor) -> NSTextField {
        let field = NSTextField(labelWithString: text)
        field.font = .systemFont(ofSize: size, weight: bold ? .semibold : .regular)
        field.textColor = color
        field.lineBreakMode = .byTruncatingTail
        return field
    }

    private func button(_ title: String, action: Selector) -> NSButton {
        let button = NSButton(title: title, target: self, action: action)
        button.bezelStyle = .rounded
        return button
    }

    private func configure(_ button: NSButton, action: Selector) {
        button.target = self
        button.action = action
        button.bezelStyle = .rounded
    }
}
