# SZORM
C# ORM,支持mssql,mysql,oracle,sqlite

## 使用方式

#### 定义一个基础类

```
/// <summary>
/// 基础字段
/// </summary>
public class Basic
{
    /// <summary>
    /// 主键
    /// </summary>
    [SZColumn(MaxLength = 32, IsKey = true)]
    public string Key { get; set; }
    /// <summary>
    /// 添加时间,首次添加记录会自动更新
    /// </summary>
    [SZColumn(IsAddTime = true)]
    public DateTime? AddTime { get; set; }
    /// <summary>
    /// 修改时间,该时间会每次更新的时候自动更新
    /// </summary>
    [SZColumn(IsEditTime = true)]
    public DateTime? EditTime { get; set; }
}
```
#### 定义一个实体类
```
public class UserInfo:Basic
{
    /// <summary>
    /// 登录用户名
    /// </summary>
    [SZColumn(MaxLength = 100)]
    public string Name { get; set; }
    /// <summary>
    /// 手机号
    /// </summary>
    [SZColumn(MaxLength = 11)]
    public string Tel { get; set; }
    /// <summary>
    /// 是否锁定,如果锁定后,将不能登录
    /// </summary>
    public bool IsLock { get => isLock; set => isLock = value; }

    private bool isLock = false;
    /// <summary>
    /// 备注信息
    /// </summary>
    public string Remarks { get; set; }
}
```

#### app.config或者web.config文件中添加
```
<system.data>
    <DbProviderFactories>
      <remove invariant="MySql.Data.MySqlClient" />
      <add name="MySQL Data Provider" invariant="MySql.Data.MySqlClient" description=".Net Framework Data Provider for MySQL" type="MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data,  Version=6.9.6.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d" />
      <remove invariant="Oracle.ManagedDataAccess.Client" />
      <add name="Oracle Data Provider" invariant="Oracle.ManagedDataAccess.Client" description=".Net Framework Data Provider for Oracle" type="Oracle.ManagedDataAccess.Client.OracleClientFactory,Oracle.ManagedDataAccess, Version=4.121.2.0, Culture=neutral, PublicKeyToken=89b483f429c47342" />
      <remove invariant="System.Data.SQLite" />
      <add name="SQLite Data Provider" invariant="System.Data.SQLite" description=".Net Framework Data Provider for SQLite" type="System.Data.SQLite.SQLiteFactory, System.Data.SQLite, Version=1.0.93.0, Culture=neutral, PublicKeyToken=db937bc2d44ff139" />
    </DbProviderFactories>
  </system.data>
  ```

  #### 添加连接字符串,如果连接其他的数据库,请采用其他的数据库连接字符串
```
  <connectionStrings>
    <add name="DB" connectionString="Database='jk';Data Source='127.0.0.1';User Id='root';Password='1234';charset='utf8mb4';pooling=false" providerName="MySql.Data.MySqlClient" />
  </connectionStrings>
  ```

  #### 继承DbContext,类名称与字符串名称一致"DB"
```
public class DB : DbContext
{
    /// <summary>
    /// 用户信息,用于登录
    /// </summary>
    public DbSet<UserInfo> UserInfos { get; set; }
    protected override void Initialization()
    {
    }
    protected override void UpdataDBExce()
    {
    }
}
```
#### ok大功告成,您可以使用啦.

## 常用方法

#### 添加
```
using (DB db = new DB())
{
    UserInfo model=new UserInfo();
    model.Name="张三";
    model.Tel="1888888888";
    model.Remarks="测试";
    db.UserInfos.Add(model);
    db.Save();
}
```

#### 修改
```
using (DB db = new DB())
{
    UserInfo model=db.UserInfos.Find(key)
    model.Name="张三";
    model.Tel="1888888888";
    model.Remarks="测试";
    db.UserInfos.Edit(model);
    db.Save();
}
```

#### 删除
```
using (DB db = new DB())
{
    db.UserInfos.Remove(key)
    db.Save();
}
```

#### 单表查询
```
using (DB db = new DB())
{
    var query = db.UserInfos.AsQuery();
    query = query.Where(w => w.LoginName.Contains("张"));
    query = query.OrderByDesc(o => o.AddTime);
    var result= query.ToList();
}
```