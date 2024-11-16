# バッチアプリケーション サンプル

## 概要
このプロジェクトは、Web API からデータを取得し、PostgreSQL データベースに保存するバッチアプリケーションです。
外部APIからの天気データの取得と、データベースへの保存を行います。

## 開発環境の準備

### 前提条件
- .NET SDK 8.0.404 以上
  - [.NET SDK のダウンロード](https://dotnet.microsoft.com/download)
  - インストール後、以下のコマンドでバージョンを確認してください
    ```bash
    dotnet --version
    ```

### 開発ツール
- Visual Studio Code
  - [ダウンロード](https://code.visualstudio.com/)
  - 必要な拡張機能:
    - C# Dev Kit
    - .NET Core Test Explorer
    - Coverage Gutters

### セットアップ手順

1. リポジトリのクローン
```bash
git clone [リポジトリURL]
cd BatchApplication
```

2. 依存パッケージのインストール
```bash
# ソリューションの依存関係を復元
dotnet restore
```

3. ビルド
```bash
dotnet build
```

4. テストの実行
```bash
dotnet test
```

### VSCodeでの開発設定

1. プロジェクトを開く
```bash
code .
```

2. 必要な拡張機能のインストール
VSCodeのExtensionsメニューから以下をインストール：
- C# Dev Kit
- .NET Core Test Explorer
- Coverage Gutters

3. テストエクスプローラーの使用方法
- `Ctrl + Shift + P` でコマンドパレットを開く
- "Test: Focus on Test Explorer View" を実行
- テストエクスプローラーでテストを表示・実行

### 環境設定ファイル
プロジェクトルートに `appsettings.json` を作成し、以下の設定を行ってください：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=weatherdb;Username=your_username;Password=your_password"
  },
  "ApiClient": {
    "BaseUrl": "http://api.weather.example.com",
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:02",
    "Timeout": "00:00:30"
  }
}
```

### トラブルシューティング

#### テストが見つからない場合
1. .NET SDKのバージョンを確認
```bash
dotnet --version
```

2. プロジェクトの復元を試す
```bash
dotnet restore
```

3. テストプロジェクトのビルドを確認
```bash
dotnet build src/BatchApplication.Tests/BatchApplication.Tests.csproj
```

#### テスト実行時にエラーが発生する場合
1. 詳細なログを確認
```bash
dotnet test -v d
```

2. 特定のテストクラスのみ実行
```bash
dotnet test --filter "FullyQualifiedName~WeatherServiceTests"
```

### プロジェクト構成

```
src/
├───BatchApplication/           # メインプロジェクト
│   ├───Core/                  # ドメインロジック
│   │   ├───Common/           # 共通基盤
│   │   │   ├───ApiClient/   # API通信の共通機能
│   │   │   └───Repository/  # データアクセスの共通機能
│   │   ├───Interfaces/       # インターフェース定義
│   │   ├───Models/          # ドメインモデル
│   │   └───Services/        # ビジネスロジック
│   │
│   └───Infrastructure/       # 外部サービス連携
│       ├───ApiClients/      # API通信の実装
│       └───Repositories/    # データアクセスの実装
│
└───BatchApplication.Tests/   # テストプロジェクト
    ├───Core/                # コア層のテスト
    │   ├───Services/       # ビジネスロジックのテスト
    │   └───Models/         # モデルのテスト
    └───Infrastructure/      # インフラ層のテスト
        ├───ApiClients/     # API通信のテスト
        └───Repositories/   # データアクセスのテスト
```

### テスト実装のガイドライン

1. テストの基本方針
- 外部依存（DB、API）を持たないテストを作成
- Mockを使用して外部依存をモック化
- テストケースは境界値を考慮

2. テストの作成手順
```csharp
// テストクラスの基本構造
public class WeatherServiceTests
{
    private readonly Mock<IWeatherApiClient> _mockApiClient;
    private readonly Mock<IWeatherRepository> _mockRepository;
    private readonly Mock<ILogger<WeatherService>> _mockLogger;
    private readonly WeatherService _service;

    public WeatherServiceTests()
    {
        // テストの準備
        _mockApiClient = new Mock<IWeatherApiClient>();
        _mockRepository = new Mock<IWeatherRepository>();
        _mockLogger = new Mock<ILogger<WeatherService>>();
        
        _service = new WeatherService(
            _mockApiClient.Object,
            _mockRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task TestMethod_Scenario_ExpectedBehavior()
    {
        // Arrange
        // テストデータとモックの設定

        // Act
        // テスト対象メソッドの実行

        // Assert
        // 結果の検証
    }
}
```

### コーディング規約
- インデントはスペース4つを使用
- private フィールドには _ プレフィックスを付与
- 非同期メソッドには Async サフィックスを付与
- インターフェースには I プレフィックスを付与
- メソッド名は動詞から開始（例：GetWeatherData）

### マージリクエストのガイドライン
1. コーディング規約に準拠していること
2. 新機能の追加時は、必ずテストを作成すること
3. 既存のテストがすべてパスすること
4. コードレビューを1名以上から得ること

### CI/CD

現在実装されているCI/CDの手順：

```bash
# ビルドとテスト
dotnet restore
dotnet build
dotnet test

# コードカバレッジの取得
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### 作者
kudarizakawonobore@gmail.com