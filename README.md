# Revit_Addin_TEST

電気設備-配管クリアランスチェック用のRevitアドイン。詳細仕様は `spec_revit (1).md` を参照。

## 構成

```
src/CheckAddin/
├── CheckAddin.csproj                       # .NET 8 / Revit 2027 API参照
├── CheckAddin.addin                         # Add-inマニフェスト
├── Commands/CheckClearanceCommand.cs        # IExternalCommand本体
├── Checks/ElectricalPipeClearanceChecker.cs # 判定ロジック(電気設備直上の配管を検出)
├── Models/ClearanceResult.cs                # NG結果1件分のデータ
└── Views/ClearanceCheckWindow.xaml(.cs)     # 距離入力+実行+結果一覧のWPFウィンドウ
```

## ビルド方法

Windows + Visual Studio 2022以降(または `dotnet` CLI)、Revit 2027がインストールされた環境が必要です。

```
dotnet build src/CheckAddin/CheckAddin.csproj -c Release
```

Revitのインストール先が既定(`C:\Program Files\Autodesk\Revit 2027`)と異なる場合は以下のように指定します。

```
dotnet build src/CheckAddin/CheckAddin.csproj -c Release -p:RevitInstallDir="D:\Autodesk\Revit 2027"
```

## Revitへの登録

1. ビルド後に生成される `CheckAddin.dll` と `CheckAddin.addin` を1つのフォルダにまとめて配置します。
2. `CheckAddin.addin` を `%APPDATA%\Autodesk\Revit\Addins\2027\` にコピーします(`<Assembly>` パスは配置先の `CheckAddin.dll` を指すよう調整してください)。
3. Revit 2027を起動すると、「アドイン」タブに「配管クリアランスチェック」ボタンが追加されます。

## 使い方

1. Revitで「アドイン」タブ→「配管クリアランスチェック」をクリック
2. ウィンドウが開くので判定距離(m)を入力し「実行」を押す
3. 現在のビュー/レベルにある電気設備(OST_ElectricalFixtures)と配管(OST_PipeCurves)をBoundingBoxで簡易判定し、NGペアを一覧表示
4. 一覧の行をダブルクリックすると該当要素がRevit画面上で選択・ズームされる
