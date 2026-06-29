using ISO11820Simulator.Config;
using ISO11820Simulator.Models;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace ISO11820Simulator.Data;

public sealed class DbHelper : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connStr;

    public DbHelper(string dbPath)
    {
        _dbPath = PathResolver.Resolve(dbPath);
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _connStr = $"Data Source={_dbPath}";
    }

    public void InitializeDatabase()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SchemaSql;
        cmd.ExecuteNonQuery();
        SeedInitialData(conn);
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    private void SeedInitialData(SqliteConnection conn)
    {
        Execute(conn, "INSERT INTO operators (userid, username, pwd, usertype) SELECT '1','admin','123456','admin' WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username='admin');");
        Execute(conn, "INSERT INTO operators (userid, username, pwd, usertype) SELECT '2','experimenter','123456','operator' WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username='experimenter');");
        Execute(conn, "INSERT INTO apparatus (apparatusid,innernumber,apparatusname,checkdatef,checkdatet,pidport,powerport,constpower) SELECT 0,'FURNACE-01','一号试验炉',date('now'),date('now','+1 year'),'COM9','COM9',2048 WHERE NOT EXISTS (SELECT 1 FROM apparatus WHERE apparatusid=0);");
        for (var i = 0; i <= 16; i++)
        {
            var name = i switch { 0 => "炉温1", 1 => "炉温2", 2 => "表面温度", 3 => "中心温度", 16 => "校准温度", _ => $"备用通道{i + 1}" };
            var group = i == 16 ? "校准" : "采集";
            Execute(conn, $"INSERT INTO sensors (sensorid,sensorname,dispname,sensorgroup,unit,discription,flag,signalzero,signalspan,outputzero,outputspan,outputvalue,inputvalue,signaltype) SELECT {i},'Sensor{i}','{name}','{group}','℃','{name}','启用',0,0,0,1000,0,0,4 WHERE NOT EXISTS (SELECT 1 FROM sensors WHERE sensorid={i});");
        }
    }

    private static void Execute(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public bool Login(string username, string pwd, out UserSession? session)
    {
        session = null;
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT userid, username, usertype FROM operators WHERE username=$name AND pwd=$pwd LIMIT 1";
        cmd.Parameters.AddWithValue("$name", username);
        cmd.Parameters.AddWithValue("$pwd", pwd);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return false;
        session = new UserSession(reader.GetString(0), reader.GetString(1), reader.GetString(2));
        return true;
    }

    public List<string> GetOperators()
    {
        var list = new List<string>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT username FROM operators ORDER BY usertype, username";
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) list.Add(reader.GetString(0));
        return list;
    }

    public (string InnerNumber, string Name, DateTime CheckDateTo, int ConstPower) GetDefaultApparatus()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT innernumber, apparatusname, checkdatet, COALESCE(constpower,2048) FROM apparatus ORDER BY apparatusid LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return ("FURNACE-01", "一号试验炉", DateTime.Today.AddYears(1), 2048);
        return (reader.GetString(0), reader.GetString(1), DateTime.Parse(reader.GetString(2)), reader.GetInt32(3));
    }

    public void CreateTest(TestSession test)
    {
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        using (var product = conn.CreateCommand())
        {
            product.Transaction = tx;
            product.CommandText = @"
INSERT INTO productmaster(productid, productname, specific, diameter, height, flag)
VALUES($pid,$name,$specific,$diameter,$height,'')
ON CONFLICT(productid) DO UPDATE SET productname=$name, specific=$specific, diameter=$diameter, height=$height";
            product.Parameters.AddWithValue("$pid", test.ProductId);
            product.Parameters.AddWithValue("$name", test.ProductName);
            product.Parameters.AddWithValue("$specific", test.Specific);
            product.Parameters.AddWithValue("$diameter", test.Diameter);
            product.Parameters.AddWithValue("$height", test.Height);
            product.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
INSERT INTO testmaster
(productid,testid,testdate,ambtemp,ambhumi,according,operator,apparatusid,apparatusname,apparatuschkdate,rptno,
 preweight,postweight,lostweight,lostweight_per,totaltesttime,constpower,phenocode,flametime,flameduration,
 maxtf1,maxtf2,maxts,maxtc,maxtf1_time,maxtf2_time,maxts_time,maxtc_time,
 finaltf1,finaltf2,finalts,finaltc,finaltf1_time,finaltf2_time,finalts_time,finaltc_time,
 deltatf1,deltatf2,deltatf,deltats,deltatc,memo,flag)
VALUES
($pid,$tid,date('now'),$ambt,$ambh,$according,$op,$appid,$appname,$chk,$rpt,
 $pre,0,0,0,0,$power,'',0,0,
 0,0,0,0,0,0,0,0,
 0,0,0,0,0,0,0,0,
 0,0,0,0,0,'','')";
            cmd.Parameters.AddWithValue("$pid", test.ProductId);
            cmd.Parameters.AddWithValue("$tid", test.TestId);
            cmd.Parameters.AddWithValue("$ambt", test.AmbientTemperature);
            cmd.Parameters.AddWithValue("$ambh", test.AmbientHumidity);
            cmd.Parameters.AddWithValue("$according", test.According);
            cmd.Parameters.AddWithValue("$op", test.Operator);
            cmd.Parameters.AddWithValue("$appid", test.ApparatusId);
            cmd.Parameters.AddWithValue("$appname", test.ApparatusName);
            cmd.Parameters.AddWithValue("$chk", test.ApparatusCheckDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$rpt", string.IsNullOrWhiteSpace(test.ReportNo) ? test.ProductId : test.ReportNo);
            cmd.Parameters.AddWithValue("$pre", test.PreWeight);
            cmd.Parameters.AddWithValue("$power", test.ConstPower);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public void UpdateTestResult(TestSession test, TestResult result)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE testmaster SET
 postweight=$post,lostweight=$lost,lostweight_per=$lostper,totaltesttime=$total,constpower=$power,phenocode=$pheno,
 flametime=$flametime,flameduration=$flameduration,maxtf1=$maxtf1,maxtf2=$maxtf2,maxts=$maxts,maxtc=$maxtc,
 maxtf1_time=$maxtf1t,maxtf2_time=$maxtf2t,maxts_time=$maxtst,maxtc_time=$maxtct,
 finaltf1=$finaltf1,finaltf2=$finaltf2,finalts=$finalts,finaltc=$finaltc,
 finaltf1_time=$finaltf1t,finaltf2_time=$finaltf2t,finalts_time=$finaltst,finaltc_time=$finaltct,
 deltatf1=$deltatf1,deltatf2=$deltatf2,deltatf=$deltatf,deltats=$deltats,deltatc=$deltatc,memo=$memo,flag='10000000'
WHERE productid=$pid AND testid=$tid";
        cmd.Parameters.AddWithValue("$post", result.PostWeight);
        cmd.Parameters.AddWithValue("$lost", result.LostWeight);
        cmd.Parameters.AddWithValue("$lostper", result.LostWeightPercent);
        cmd.Parameters.AddWithValue("$total", result.TotalTestTime);
        cmd.Parameters.AddWithValue("$power", result.ConstPower);
        cmd.Parameters.AddWithValue("$pheno", result.PhenoCode);
        cmd.Parameters.AddWithValue("$flametime", result.FlameTime);
        cmd.Parameters.AddWithValue("$flameduration", result.FlameDuration);
        cmd.Parameters.AddWithValue("$maxtf1", result.MaxTf1);
        cmd.Parameters.AddWithValue("$maxtf2", result.MaxTf2);
        cmd.Parameters.AddWithValue("$maxts", result.MaxTs);
        cmd.Parameters.AddWithValue("$maxtc", result.MaxTc);
        cmd.Parameters.AddWithValue("$maxtf1t", result.MaxTf1Time);
        cmd.Parameters.AddWithValue("$maxtf2t", result.MaxTf2Time);
        cmd.Parameters.AddWithValue("$maxtst", result.MaxTsTime);
        cmd.Parameters.AddWithValue("$maxtct", result.MaxTcTime);
        cmd.Parameters.AddWithValue("$finaltf1", result.FinalTf1);
        cmd.Parameters.AddWithValue("$finaltf2", result.FinalTf2);
        cmd.Parameters.AddWithValue("$finalts", result.FinalTs);
        cmd.Parameters.AddWithValue("$finaltc", result.FinalTc);
        cmd.Parameters.AddWithValue("$finaltf1t", result.FinalTf1Time);
        cmd.Parameters.AddWithValue("$finaltf2t", result.FinalTf2Time);
        cmd.Parameters.AddWithValue("$finaltst", result.FinalTsTime);
        cmd.Parameters.AddWithValue("$finaltct", result.FinalTcTime);
        cmd.Parameters.AddWithValue("$deltatf1", result.DeltaTf1);
        cmd.Parameters.AddWithValue("$deltatf2", result.DeltaTf2);
        cmd.Parameters.AddWithValue("$deltatf", result.DeltaTf);
        cmd.Parameters.AddWithValue("$deltats", result.DeltaTs);
        cmd.Parameters.AddWithValue("$deltatc", result.DeltaTc);
        cmd.Parameters.AddWithValue("$memo", result.Memo);
        cmd.Parameters.AddWithValue("$pid", test.ProductId);
        cmd.Parameters.AddWithValue("$tid", test.TestId);
        cmd.ExecuteNonQuery();
    }

    public List<TestRecordSummary> QueryTests(DateTime from, DateTime to, string productId, string operatorName)
    {
        var list = new List<TestRecordSummary>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT t.productid,t.testid,t.testdate,t.operator,p.productname,t.lostweight_per,t.deltatf,t.totaltesttime,COALESCE(t.flag,''),t.flameduration
FROM testmaster t LEFT JOIN productmaster p ON p.productid=t.productid
WHERE date(t.testdate) BETWEEN date($from) AND date($to)
  AND ($pid='' OR t.productid LIKE '%' || $pid || '%')
  AND ($op='' OR t.operator=$op)
ORDER BY t.testdate DESC, t.testid DESC";
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$pid", productId ?? string.Empty);
        cmd.Parameters.AddWithValue("$op", operatorName ?? string.Empty);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new TestRecordSummary
            {
                ProductId = reader.GetString(0),
                TestId = reader.GetString(1),
                TestDate = DateTime.Parse(reader.GetString(2)),
                Operator = reader.GetString(3),
                ProductName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                LostWeightPercent = reader.GetDouble(5),
                DeltaTf = reader.GetDouble(6),
                TotalTestTime = reader.GetInt32(7),
                Flag = reader.GetString(8),
                FlameDuration = reader.GetInt32(9)
            });
        }
        return list;
    }

    public Dictionary<string, object?> GetTestDetail(string productId, string testId)
    {
        var dict = new Dictionary<string, object?>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM testmaster WHERE productid=$pid AND testid=$tid LIMIT 1";
        cmd.Parameters.AddWithValue("$pid", productId);
        cmd.Parameters.AddWithValue("$tid", testId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return dict;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            dict[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }
        return dict;
    }

    public void SaveCalibration(IEnumerable<double> values, string type, string operatorName, string remarks)
    {
        var data = values.ToList();
        if (data.Count == 0) throw new InvalidOperationException("没有可保存的校准数据");
        var avg = data.Average();
        var maxDev = data.Max(v => Math.Abs(v - avg));
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO CalibrationRecords(Id,CalibrationDate,CalibrationType,ApparatusId,Operator,TemperatureData,UniformityResult,MaxDeviation,AverageTemperature,PassedCriteria,Remarks,CreatedAt,TempA1,TempA2,TempA3,TempB1,TempB2,TempB3,TempC1,TempC2,TempC3,TAvg,Memo)
VALUES($id,$date,$type,0,$op,$json,$uniform,$dev,$avg,$pass,$remarks,$created,$a1,$a2,$a3,$b1,$b2,$b3,$c1,$c2,$c3,$avg,$memo)";
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        cmd.Parameters.AddWithValue("$date", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("$type", type);
        cmd.Parameters.AddWithValue("$op", operatorName);
        cmd.Parameters.AddWithValue("$json", JsonSerializer.Serialize(data));
        cmd.Parameters.AddWithValue("$uniform", maxDev);
        cmd.Parameters.AddWithValue("$dev", maxDev);
        cmd.Parameters.AddWithValue("$avg", avg);
        cmd.Parameters.AddWithValue("$pass", maxDev <= 5 ? 1 : 0);
        cmd.Parameters.AddWithValue("$remarks", remarks);
        cmd.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
        var calibrationParams = new[] { "$a1", "$a2", "$a3", "$b1", "$b2", "$b3", "$c1", "$c2", "$c3" };
        for (var i = 0; i < calibrationParams.Length; i++)
        {
            object value = i < data.Count ? data[i] : DBNull.Value;
            cmd.Parameters.AddWithValue(calibrationParams[i], value);
        }
        cmd.Parameters.AddWithValue("$memo", remarks);
        cmd.ExecuteNonQuery();
    }

    public List<CalibrationRecord> QueryCalibrations()
    {
        var list = new List<CalibrationRecord>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id,CalibrationDate,CalibrationType,Operator,TemperatureData,MaxDeviation,AverageTemperature,PassedCriteria,Remarks,CreatedAt FROM CalibrationRecords ORDER BY CalibrationDate DESC LIMIT 100";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CalibrationRecord
            {
                Id = reader.GetString(0),
                CalibrationDate = DateTime.Parse(reader.GetString(1)),
                CalibrationType = reader.GetString(2),
                Operator = reader.GetString(3),
                TemperatureData = reader.GetString(4),
                MaxDeviation = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                AverageTemperature = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                PassedCriteria = reader.GetInt32(7) == 1,
                Remarks = reader.GetString(8),
                CreatedAt = DateTime.Parse(reader.GetString(9))
            });
        }
        return list;
    }

    public void Dispose()
    {
        // Connections are opened per operation; no pooled connection is held here.
    }

    private const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS operators (userid TEXT NOT NULL, username TEXT NOT NULL, pwd TEXT NOT NULL, usertype TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS apparatus (apparatusid INTEGER NOT NULL CONSTRAINT PK_apparatus PRIMARY KEY, innernumber TEXT NOT NULL, apparatusname TEXT NOT NULL, checkdatef date NOT NULL, checkdatet date NOT NULL, pidport TEXT NOT NULL, powerport TEXT NOT NULL, constpower INTEGER NULL);
CREATE TABLE IF NOT EXISTS productmaster (productid TEXT NOT NULL CONSTRAINT PK_productmaster PRIMARY KEY, productname TEXT NOT NULL, specific TEXT NOT NULL, diameter REAL NOT NULL, height REAL NOT NULL, flag TEXT NULL);
CREATE TABLE IF NOT EXISTS testmaster (
 productid TEXT NOT NULL, testid TEXT NOT NULL, testdate date NOT NULL, ambtemp REAL NOT NULL, ambhumi REAL NOT NULL, according TEXT NOT NULL, operator TEXT NOT NULL,
 apparatusid TEXT NOT NULL, apparatusname TEXT NOT NULL, apparatuschkdate date NOT NULL, rptno TEXT NOT NULL,
 preweight REAL NOT NULL, postweight REAL NOT NULL, lostweight REAL NOT NULL, lostweight_per REAL NOT NULL,
 totaltesttime INTEGER NOT NULL, constpower INTEGER NOT NULL, phenocode TEXT NOT NULL, flametime INTEGER NOT NULL, flameduration INTEGER NOT NULL,
 maxtf1 REAL NOT NULL, maxtf2 REAL NOT NULL, maxts REAL NOT NULL, maxtc REAL NOT NULL,
 maxtf1_time INTEGER NOT NULL, maxtf2_time INTEGER NOT NULL, maxts_time INTEGER NOT NULL, maxtc_time INTEGER NOT NULL,
 finaltf1 REAL NOT NULL, finaltf2 REAL NOT NULL, finalts REAL NOT NULL, finaltc REAL NOT NULL,
 finaltf1_time INTEGER NOT NULL, finaltf2_time INTEGER NOT NULL, finalts_time INTEGER NOT NULL, finaltc_time INTEGER NOT NULL,
 deltatf1 REAL NOT NULL, deltatf2 REAL NOT NULL, deltatf REAL NOT NULL, deltats REAL NOT NULL, deltatc REAL NOT NULL,
 memo TEXT NULL, flag TEXT NULL,
 CONSTRAINT PK_testmaster PRIMARY KEY(productid,testid), CONSTRAINT FK_testmaster_productmaster FOREIGN KEY(productid) REFERENCES productmaster(productid)
);
CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate ON testmaster(testdate);
CREATE INDEX IF NOT EXISTS IX_Testmaster_Operator ON testmaster(operator);
CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate_Productid ON testmaster(testdate, productid);
CREATE TABLE IF NOT EXISTS sensors (sensorid INTEGER NOT NULL CONSTRAINT PK_sensors PRIMARY KEY, sensorname TEXT NOT NULL, dispname TEXT NOT NULL, sensorgroup TEXT NOT NULL, unit TEXT NOT NULL, discription TEXT NOT NULL, flag TEXT NOT NULL, signalzero REAL NOT NULL, signalspan REAL NOT NULL, outputzero REAL NOT NULL, outputspan REAL NOT NULL, outputvalue REAL NOT NULL, inputvalue REAL NOT NULL, signaltype INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS CalibrationRecords (Id TEXT NOT NULL CONSTRAINT PK_CalibrationRecords PRIMARY KEY, CalibrationDate TEXT NOT NULL, CalibrationType TEXT NOT NULL, ApparatusId INTEGER NOT NULL, Operator TEXT NOT NULL, TemperatureData TEXT NOT NULL, UniformityResult REAL NULL, MaxDeviation REAL NULL, AverageTemperature REAL NULL, PassedCriteria INTEGER NOT NULL, Remarks TEXT NOT NULL, CreatedAt TEXT NOT NULL, TempA1 REAL NULL, TempA2 REAL NULL, TempA3 REAL NULL, TempB1 REAL NULL, TempB2 REAL NULL, TempB3 REAL NULL, TempC1 REAL NULL, TempC2 REAL NULL, TempC3 REAL NULL, TAvg REAL NULL, TAvgAxis1 REAL NULL, TAvgAxis2 REAL NULL, TAvgAxis3 REAL NULL, TAvgLevela REAL NULL, TAvgLevelb REAL NULL, TAvgLevelc REAL NULL, TDevAxis1 REAL NULL, TDevAxis2 REAL NULL, TDevAxis3 REAL NULL, TDevLevela REAL NULL, TDevLevelb REAL NULL, TDevLevelc REAL NULL, TAvgDevAxis REAL NULL, TAvgDevLevel REAL NULL, CenterTempData TEXT NULL, Memo TEXT NULL);
CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Date ON CalibrationRecords(CalibrationDate);
CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Operator ON CalibrationRecords(Operator);
";
}
