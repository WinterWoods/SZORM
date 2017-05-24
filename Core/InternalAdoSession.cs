using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SZORM.Exceptions;
using SZORM.Infrastructure;
using SZORM.InternalExtensions;

namespace SZORM.Core
{
    class InternalAdoSession : IDisposable
    {
        IDbConnection _dbConnection;
        IDbTransaction _dbTransaction;
        bool _isInTransaction;
        int _commandTimeout = 30;/* seconds */


        bool _disposed = false;


        public bool IsInTransaction { get { return this._isInTransaction; } }
        public int CommandTimeout { get { return this._commandTimeout; } set { this._commandTimeout = value; } }

        public InternalAdoSession(IDbConnection conn)
        {
            _dbConnection = conn;
        }

        public void Activate()
        {
            this.CheckDisposed();

            if (this._dbConnection.State == ConnectionState.Broken)
            {
                this._dbConnection.Close();
            }

            if (this._dbConnection.State == ConnectionState.Closed)
            {
                this._dbConnection.Open();
            }
        }
        public void Complete()
        {
            if (!this._isInTransaction)
            {
                if (this._dbConnection.State == ConnectionState.Open)
                {
                    this._dbConnection.Close();
                }
            }
        }
        public void BeginTransaction()
        {
            this.Activate();
            this._dbTransaction = this._dbConnection.BeginTransaction();
            this._isInTransaction = true;
        }
        public void BeginTransaction(IsolationLevel il)
        {
            this.Activate();
            this._dbTransaction = this._dbConnection.BeginTransaction(il);
            this._isInTransaction = true;
        }
        public void CommitTransaction()
        {
            if (!this._isInTransaction)
            {
                throw new SZORMException("Current session does not open a transaction.");
            }
            this._dbTransaction.Commit();
            this.ReleaseTransaction();
        }
        public void RollbackTransaction()
        {
            if (!this._isInTransaction)
            {
                throw new SZORMException("Current session does not open a transaction.");
            }
            this._dbTransaction.Rollback();
            this.ReleaseTransaction();
        }
        void ReleaseTransaction()
        {
            this._dbTransaction.Dispose();
            this._dbTransaction = null;
            this._isInTransaction = false;
        }
        public IDataReader ExecuteReader(string cmdText, DbParam[] parameters, CommandType cmdType)
        {
            return this.ExecuteReader(cmdText, parameters, cmdType, CommandBehavior.Default);
        }
        public IDataReader ExecuteReader(string cmdText, DbParam[] parameters, CommandType cmdType, CommandBehavior behavior)
        {
            this.CheckDisposed();

            IDbCommand cmd = this.PrepareCommand(cmdText, parameters, cmdType);
            
            this.Activate();

            IDataReader reader;
            try
            {
                reader = new InternalDataReader(this, cmd.ExecuteReader(behavior), cmd);
            }
            catch (Exception ex)
            {
                throw WrapException(ex);
            }
            
            return reader;
        }
        public int ExecuteNonQuery(string cmdText, DbParam[] parameters, CommandType cmdType)
        {
            this.CheckDisposed();

            IDbCommand cmd = null;
            try
            {
                cmd = this.PrepareCommand(cmdText, parameters, cmdType);


                this.Activate();

                int rowsAffected;
                try
                {
                    rowsAffected = cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw WrapException(ex);
                }
                return rowsAffected;
            }
            finally
            {
                this.Complete();
                if (cmd != null)
                    cmd.Dispose();
            }
        }
        public object ExecuteScalar(string cmdText, DbParam[] parameters, CommandType cmdType)
        {
            this.CheckDisposed();

            IDbCommand cmd = null;
            try
            {
                cmd = this.PrepareCommand(cmdText, parameters, cmdType);

                this.Activate();
                object ret;
                try
                {
                    ret = cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    throw WrapException(ex);
                }
                return ret;
            }
            finally
            {
                this.Complete();
                if (cmd != null)
                    cmd.Dispose();
            }
        }


        public void Dispose()
        {
            if (this._disposed)
                return;

            if (this._dbTransaction != null)
            {
                if (this._isInTransaction)
                {
                    try
                    {
                        this._dbTransaction.Rollback();
                    }
                    catch
                    {
                    }
                }

                this.ReleaseTransaction();
            }

            if (this._dbConnection != null)
            {
                this._dbConnection.Dispose();
            }

            this._disposed = true;
        }

        IDbCommand PrepareCommand(string cmdText, DbParam[] parameters, CommandType cmdType)
        {
            IDbCommand cmd = this._dbConnection.CreateCommand();

            cmd.CommandText = cmdText;
            cmd.CommandType = cmdType;
            cmd.CommandTimeout = this._commandTimeout;
            if (this.IsInTransaction)
                cmd.Transaction = this._dbTransaction;

            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    DbParam param = parameters[i];
                    if (param == null)
                        continue;

                    if (param.ExplicitParameter != null)/* 如果存在创建好了的 IDbDataParameter，则直接用它。同时也忽视了 DbParam 的其他属性 */
                    {
                        cmd.Parameters.Add(param.ExplicitParameter);
                        continue;
                    }

                    IDbDataParameter parameter = cmd.CreateParameter();
                    parameter.ParameterName = param.Name;

                    Type parameterType;
                    if (param.Value == null || param.Value == DBNull.Value)
                    {
                        parameter.Value = DBNull.Value;
                        parameterType = param.Type;
                    }
                    else
                    {
                        parameter.Value = param.Value;
                        parameterType = param.Value.GetType();
                    }

                    if (param.Precision != null)
                        parameter.Precision = param.Precision.Value;

                    if (param.Scale != null)
                        parameter.Scale = param.Scale.Value;

                    if (param.Size != null)
                        parameter.Size = param.Size.Value;

                    if (param.DbType != null)
                        parameter.DbType = param.DbType.Value;
                    else
                    {
                        DbType? dbType = MappingTypeSystem.GetDbType(parameterType);
                        if (dbType != null)
                            parameter.DbType = dbType.Value;
                    }
                    cmd.Parameters.Add(parameter);
                }
            }

            return cmd;
        }
        public static string AppendDbCommandInfo(string cmdText, DbParam[] parameters)
        {
            StringBuilder sb = new StringBuilder();
            if (parameters != null)
            {
                foreach (DbParam param in parameters)
                {
                    if (param == null)
                        continue;

                    string typeName = null;
                    object value = null;
                    Type parameterType;
                    if (param.Value == null || param.Value == DBNull.Value)
                    {
                        parameterType = param.Type;
                        value = "NULL";
                    }
                    else
                    {
                        value = param.Value;
                        parameterType = param.Value.GetType();

                        if (parameterType == typeof(string) || parameterType == typeof(DateTime))
                            value = "'" + value + "'";
                    }

                    if (parameterType != null)
                        typeName = GetTypeName(parameterType);

                    sb.AppendFormat("{0} {1} = {2};", typeName, param.Name, value);
                    sb.AppendLine();
                }
            }

            sb.AppendLine(cmdText);

            return sb.ToString();
        }
        static string GetTypeName(Type type)
        {
            Type underlyingType;
            if (ReflectionExtension.IsNullable(type, out underlyingType))
            {
                return string.Format("Nullable<{0}>", GetTypeName(underlyingType));
            }

            return type.Name;
        }
        void CheckDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
        static SZORMException WrapException(Exception ex)
        {
            return new SZORMException("执行sql发生异常:", ex);
        }
    }
}
