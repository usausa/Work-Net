namespace WorkML.Core;

using System.Globalization;

// サンプル CSV の読み込み（外部ライブラリ非依存の簡易パーサ）
public static class CsvLoader
{
    // devices.csv: DeviceId,SiteId,BaseVoltage,RatedCurrent,ChannelCount
    public static IReadOnlyList<DeviceSpec> LoadDevices(string path)
    {
        var list = new List<DeviceSpec>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var c = line.Split(',');
            list.Add(new DeviceSpec
            {
                DeviceId = c[0],
                SiteId = c[1],
                BaseVoltage = float.Parse(c[2], CultureInfo.InvariantCulture),
                RatedCurrent = float.Parse(c[3], CultureInfo.InvariantCulture)
            });
        }

        return list;
    }

    // readings.csv: DeviceId,ChannelNo,Timestamp,Value
    public static IReadOnlyList<ChannelReading> LoadReadings(string path)
    {
        var list = new List<ChannelReading>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var c = line.Split(',');
            list.Add(new ChannelReading
            {
                DeviceId = c[0],
                ChannelNo = int.Parse(c[1], CultureInfo.InvariantCulture),
                Timestamp = DateTime.Parse(c[2], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                Value = float.Parse(c[3], CultureInfo.InvariantCulture)
            });
        }

        return list;
    }
}
