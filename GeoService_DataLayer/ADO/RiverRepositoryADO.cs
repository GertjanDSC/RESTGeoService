﻿using GeoService_BusinessLayer.Interfaces;
using GeoService_BusinessLayer.Models;
using GeoService_DataLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoService_DataLayer.ADO {
    public class RiverRepositoryADO : IRiverRepository {
        private readonly string _connectionString;

        public RiverRepositoryADO(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection GetConnection()
        {
            SqlConnection connection = new(_connectionString);
            return connection;
        }

        public List<Country> GeefLandenRivier(int riverId)
        {
            string sql =
                "SELECT CountryRiver.*, Country.ContinentId,Country.Name AS CountryName,Country.Population, Country.Surface, Continent.Name FROM[dbo].[CountryRiver] CountryRiver " +
                "INNER JOIN[dbo].[Country] Country ON Country.CountryId = CountryRiver.CountryId " +
                "INNER JOIN[dbo].[Continent] Continent ON Continent.ContinentId = Country.ContinentId " +
                "WHERE RiverId = @riverId";
            SqlConnection connection = GetConnection();
            using SqlCommand command = new(sql, connection);
            try
            {
                connection.Open();
                Continent continent = null;
                List<Country> countries = new();
                command.Parameters.AddWithValue("@riverId", riverId);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    //zolang hij de naam nog niet heeft
                    if (true)
                    {
                        continent = new((string)reader["Name"], (int)reader["ContinentId"]);
                    }
                    Country country = new((int)reader["CountryId"], (string)reader["CountryName"], (int)reader["Population"], (decimal)reader["Surface"], continent);
                    countries.Add(country);
                }
                return countries;
            }
            catch (Exception ex)
            {
                throw new RiverRepositoryADOException("GeefLandenContinentADO - error", ex);
            }
            finally
            {
                connection.Close();
            }
        }
        public River RivierWeergeven(int riverId)
        {
            River rivier = null;
            string sql = "SELECT * FROM [dbo].[River] WHERE RiverId = @RiverId";
            SqlConnection connection = GetConnection();
            using SqlCommand command = new(sql, connection);
            try
            {
                connection.Open();
                command.Parameters.AddWithValue("@RiverId", riverId);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rivier = new((string)reader["Name"], (int)reader["Length"]);
                }
                reader.Close();
                return rivier;
            }
            catch (Exception ex)
            {
                throw new RiverRepositoryADOException("RivierWeergeven - error", ex);
            }
            finally
            {
                connection.Close();
            }
        }
        public bool BestaatRivier(int riverId)
        {
            string sql = "SELECT COUNT(*) FROM [dbo].[River] WHERE RiverId = @RiverId";
            SqlConnection connection = GetConnection();
            using SqlCommand command = new(sql, connection);
            try
            {
                connection.Open();
                command.Parameters.AddWithValue("@RiverId", riverId);
                int n = (int)command.ExecuteScalar();
                if (n > 0) return true;
                return false;
            }
            catch (Exception ex)
            {
                throw new RiverRepositoryADOException("BestaatContinentADO - error", ex);
            }
            finally
            {
                connection.Close();
            }
        }
        public bool BestaatRivier(string name)
        {
            string sql = "SELECT COUNT(*) FROM [dbo].[River] WHERE Name = @Name";
            SqlConnection connection = GetConnection();
            using SqlCommand command = new(sql, connection);
            try
            {
                connection.Open();
                command.Parameters.AddWithValue("@Name", name);
                int n = (int)command.ExecuteScalar();
                if (n > 0) return true;
                return false;
            }
            catch (Exception ex)
            {
                throw new RiverRepositoryADOException("BestaatContinentADO - error", ex);
            }
            finally
            {
                connection.Close();
            }
        }
        public River RivierToevoegen(River river)
        {
            int rivierId;
            var landen = river.GetCountries();
            SqlTransaction trans = null;
            string sql = "INSERT INTO [dbo].[River] (Name, Length) OUTPUT INSERTED.RiverId VALUES (@Name, @Length)";
            SqlConnection conn = GetConnection();
            using (SqlCommand cmd2 = conn.CreateCommand())
            using (SqlCommand cmd = conn.CreateCommand())
            {
                try
                {
                    conn.Open();
                    trans = conn.BeginTransaction();
                    cmd.CommandText = sql;
                    cmd.Transaction = trans;
                    cmd.Parameters.AddWithValue("@Name", river.Name);
                    cmd.Parameters.AddWithValue("@Length", river.Length);
                    rivierId = (int)cmd.ExecuteScalar();
                    river.ZetId(rivierId);
                    foreach (var x in landen)
                    {
                        string sql2 = "INSERT INTO [dbo].CountryRiver (countryId, RiverId) VALUES (@countryId, @RiverId)";
                        cmd2.Parameters.Clear();
                        cmd2.Transaction = trans;
                        cmd2.CommandText = sql2;
                        cmd2.Parameters.AddWithValue("@countryId", x.Id);
                        cmd2.Parameters.AddWithValue("@RiverId", rivierId);
                        cmd2.ExecuteNonQuery();
                    }
                    trans.Commit();
                    return river;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    throw new RiverRepositoryADOException("BestellingToevoegen - ", ex);
                }
                finally
                {
                    conn.Close();
                }
            }
        }
        public void RivierVerwijderen(int riverId)
        {
            string sql = "DELETE FROM [dbo].[CountryRiver] WHERE RiverId = @riverId";
            string sql2 = "DELETE FROM [dbo].[River] WHERE RiverId = @RiverId";
            SqlConnection conn = GetConnection();
            using (SqlCommand cmd1 = conn.CreateCommand())
            using (SqlCommand cmd2 = conn.CreateCommand())
            {
                conn.Open();
                SqlTransaction sqltr = conn.BeginTransaction();
                cmd1.Transaction = sqltr;
                cmd2.Transaction = sqltr;
                try
                {
                    cmd1.CommandText = sql;
                    cmd2.CommandText = sql2;
                    cmd1.Parameters.AddWithValue("@riverId", riverId);
                    cmd1.ExecuteNonQuery();
                    cmd2.Parameters.AddWithValue("@RiverId", riverId);
                    cmd2.ExecuteNonQuery();
                    sqltr.Commit();
                }
                catch (Exception ex)
                {
                    sqltr.Rollback();
                    throw new RiverRepositoryADOException("RiverVerwijderene - ", ex);
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public void RivierUpdaten(River river)
        {
            var landen = river.GetCountries();
            string sqlOne = "UPDATE [dbo].[River] SET Name = @Name, Length = @Length WHERE RiverId = @RiverId";
            string sqlTwo = "INSERT INTO [dbo].[CountryRiver] (CountryId, RiverId) VALUES(@CountryId, @RiverId)";
            string sqlThree = "DELETE FROM [dbo].[CountryRiver] WHERE RiverId = @RiverId";


            SqlConnection conn = GetConnection();
            SqlCommand cmdOne = new(sqlOne, conn);
            SqlCommand cmdThree = new(sqlThree, conn);

            conn.Open();
            SqlTransaction sqltr = conn.BeginTransaction();
            cmdOne.Transaction = sqltr;
            cmdThree.Transaction = sqltr;

            try
            {
                cmdOne.Parameters.AddWithValue("@Name", river.Name);
                cmdOne.Parameters.AddWithValue("@Length", river.Length);
                cmdOne.Parameters.AddWithValue("@RiverId", river.Id);
                cmdOne.ExecuteNonQuery();
                cmdThree.Parameters.AddWithValue("@RiverId", river.Id);
                cmdThree.ExecuteNonQuery();
                foreach (var l in landen)
                {
                    SqlCommand cmdTwo = new(sqlTwo, conn);
                    cmdTwo.Parameters.Clear();
                    cmdTwo.Transaction = sqltr;
                    cmdTwo.Parameters.AddWithValue("@CountryId", l.Id);
                    cmdTwo.Parameters.AddWithValue("@RiverId", river.Id);
                    cmdTwo.ExecuteNonQuery();
                }

                sqltr.Commit();

            }
            catch (Exception ex)
            {
                sqltr.Rollback();
                throw new RiverRepositoryADOException("UpdateRivier - " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }

        public List<River> geefRivierenLand(int countryId)
        {
            string sql =
                "SELECT  River.* FROM[dbo].[CountryRiver] CountryRiver " +
                "INNER JOIN[dbo].[Country] Country ON Country.CountryId = CountryRiver.CountryId " +
                "INNER JOIN[dbo].[River] River ON River.RiverId = CountryRiver.RiverId " +
                "WHERE Country.CountryId = @countryId";
            SqlConnection connection = GetConnection();
            using SqlCommand command = new(sql, connection);
            try
            {
                connection.Open();
                List<River> Rivers = new();
                command.Parameters.AddWithValue("@countryId", countryId);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    River river = new((string)reader["Name"], (int)reader["Length"]);
                    river.ZetId((int)reader["RiverId"]);
                    Rivers.Add(river);
                }
                return Rivers;
            }
            catch (Exception ex)
            {
                throw new RiverRepositoryADOException("GeefLandenContinentADO - error", ex);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
