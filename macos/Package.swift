// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AGYReadyMac",
    platforms: [.macOS(.v14)],
    products: [
        .executable(name: "AGYReadyMac", targets: ["AGYReadyMac"])
    ],
    targets: [
        .target(name: "AGYReadyMacCore"),
        .executableTarget(name: "AGYReadyMac", dependencies: ["AGYReadyMacCore"]),
        .testTarget(name: "AGYReadyMacCoreTests", dependencies: ["AGYReadyMacCore"])
    ]
)
