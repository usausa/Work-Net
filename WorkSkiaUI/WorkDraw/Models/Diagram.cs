namespace WorkDraw.Models;

public class NodeModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 80;
    public double H { get; set; } = 80;
    public string Label { get; set; } = "";
    public bool IsContainer { get; set; }

    public double Cx => X + W / 2;
    public double Cy => Y + H / 2;
}

public class EdgeModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SourceId { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string Label { get; set; } = "";
}

public class DiagramDocument
{
    public List<NodeModel> Nodes { get; set; } = new();
    public List<EdgeModel> Edges { get; set; } = new();
}

public record StencilDef(
    string Type,
    string Name,
    string Category,
    string Color,
    double W,
    double H,
    bool IsContainer = false);

public static class StencilCatalog
{
    // AWS カテゴリ色: コンピューティング=橙, ストレージ=緑, DB=青, ネットワーク=紫, 統合=桃, セキュリティ=赤, 管理=桃紫, 全般=灰
    public static readonly List<StencilDef> All = new()
    {
        // コンテナ（グループ枠）
        new("vpc",          "VPC",              "コンテナ", "#8C4FFF", 360, 260, true),
        new("subnet-public","パブリックサブネット",  "コンテナ", "#7AA116", 260, 160, true),
        new("subnet-private","プライベートサブネット","コンテナ", "#00A4A6", 260, 160, true),
        new("az",           "アベイラビリティゾーン", "コンテナ", "#0073BB", 300, 200, true),

        // コンピューティング
        new("ec2",          "EC2",              "コンピューティング", "#ED7100", 72, 72),
        new("lambda",       "Lambda",           "コンピューティング", "#ED7100", 72, 72),
        new("ecs",          "ECS",              "コンピューティング", "#ED7100", 72, 72),
        new("autoscaling",  "Auto Scaling",     "コンピューティング", "#ED7100", 72, 72),

        // ストレージ
        new("s3",           "S3",               "ストレージ", "#7AA116", 72, 72),
        new("efs",          "EFS",              "ストレージ", "#7AA116", 72, 72),

        // データベース
        new("rds",          "RDS",              "データベース", "#527FFF", 72, 72),
        new("dynamodb",     "DynamoDB",         "データベース", "#527FFF", 72, 72),
        new("elasticache",  "ElastiCache",      "データベース", "#527FFF", 72, 72),

        // ネットワーク
        new("alb",          "ALB / ELB",        "ネットワーク", "#8C4FFF", 72, 72),
        new("apigw",        "API Gateway",      "ネットワーク", "#8C4FFF", 72, 72),
        new("cloudfront",   "CloudFront",       "ネットワーク", "#8C4FFF", 72, 72),
        new("route53",      "Route 53",         "ネットワーク", "#8C4FFF", 72, 72),
        new("igw",          "Internet Gateway", "ネットワーク", "#8C4FFF", 72, 72),
        new("natgw",        "NAT Gateway",      "ネットワーク", "#8C4FFF", 72, 72),

        // アプリケーション統合
        new("sqs",          "SQS",              "統合", "#E7157B", 72, 72),
        new("sns",          "SNS",              "統合", "#E7157B", 72, 72),

        // セキュリティ・管理
        new("waf",          "WAF",              "セキュリティ", "#DD344C", 72, 72),
        new("iam",          "IAM",              "セキュリティ", "#DD344C", 72, 72),
        new("cloudwatch",   "CloudWatch",       "管理", "#E7157B", 72, 72),

        // 全般
        new("user",         "ユーザー",          "全般", "#232F3E", 72, 72),
        new("internet",     "インターネット",     "全般", "#232F3E", 72, 72),
    };

    public static StencilDef? Find(string type) => All.FirstOrDefault(s => s.Type == type);

    public static IEnumerable<IGrouping<string, StencilDef>> ByCategory => All.GroupBy(s => s.Category);
}
