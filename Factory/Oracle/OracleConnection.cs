﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace SZORM.Factory.Oracle
{
    public class OracleConnection : IDbConnection, IDisposable, ICloneable
    {
        IDbConnection _dbConnection;
        public OracleConnection(IDbConnection dbConnection)
        {
            this._dbConnection = dbConnection;
        }

        public string ConnectionString
        {
            get { return this._dbConnection.ConnectionString; }
            set { this._dbConnection.ConnectionString = value; }
        }
        public int ConnectionTimeout
        {
            get { return this._dbConnection.ConnectionTimeout; }
        }
        public string Database
        {
            get { return this._dbConnection.Database; }
        }
        public ConnectionState State
        {
            get { return this._dbConnection.State; }
        }

        public IDbTransaction BeginTransaction()
        {
            return this._dbConnection.BeginTransaction();
        }
        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return this._dbConnection.BeginTransaction(il);
        }
        public void ChangeDatabase(string databaseName)
        {
            this._dbConnection.ChangeDatabase(databaseName);
        }
        public void Close()
        {
            this._dbConnection.Close();
        }
        public IDbCommand CreateCommand()
        {
            return new OracleCommand(this._dbConnection.CreateCommand());
        }
        public void Open()
        {
            this._dbConnection.Open();
        }

        public void Dispose()
        {
            this._dbConnection.Dispose();
        }
        public object Clone()
        {
            if (this._dbConnection is ICloneable)
            {
                return new OracleConnection((IDbConnection)((ICloneable)this._dbConnection).Clone());
            }

            throw new NotSupportedException();
        }
    }
}
