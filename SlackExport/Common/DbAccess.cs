using System;
using System.Configuration;
using MySqlConnector;
using NLog;
using SlackExport.Dto;

namespace SlackExport.Common
{
    public class DbAccess
    {
        // ログを出力するロガー定義
        public static Logger logger = LogManager.GetCurrentClassLogger();

        public bool Insert(DataDto dataDto)
        {
            // 実行するSQL
            var sql = "INSERT INTO diary (id, title, content, post_date, update_date)  SELECT MAX(id) + 1, @title, @content, @post_date, @update_date FROM diary";

            using (var connection = new MySqlConnection(ConfigurationManager.AppSettings["mySqlConnection"]))
            {
                using (var command = new MySqlCommand(sql, connection))
                {
                    try
                    {
                        // 接続の確立
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = sql;
                        // バインド変数をセット
                        // タイトルは「チャンネル名 + 日時 + ユーザ名」の形式にする
                        command.Parameters.AddWithValue("@title", dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user);
                        command.Parameters.AddWithValue("@content", dataDto.message);
                        command.Parameters.AddWithValue("@post_date", dataDto.ts);
                        command.Parameters.AddWithValue("@update_date", dataDto.ts);

                        var result = command.ExecuteNonQuery();
                        if (result != 1)
                        {
                            logger.Error("insert文でエラー。タイトル：" + dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user);
                        }
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        logger.Error("insert文発行で例外エラー。タイトル：" + dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user + " エラー内容：" + ex.Message + " スタックトレース：" + ex.StackTrace);
                    }
                }
            }
            return true;
        }

        public bool CheckRegistered(DataDto dataDto)
        {
            // 実行するSQL
            string sql = "SELECT COUNT(*) AS count FROM diary WHERE title = @title";
            using (var connection = new MySqlConnection(ConfigurationManager.AppSettings["mySqlConnection"]))
            {
                using (var command = new MySqlCommand(sql, connection))
                {
                    try
                    {
                        // 接続の確立
                        connection.Open();
                        command.Connection = connection;
                        command.CommandText = sql;

                        // タイトル
                        string title = dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user;

                        // バインド変数をセット
                        command.Parameters.AddWithValue("@title", title);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {

                                if (int.Parse(reader["count"].ToString()) > 0)
                                {
                                    logger.Info("登録済み：" + title);
                                    return true;
                                }
                            }
                            else
                            {
                                logger.Error("select文でエラー。：" + dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user);
                                return true;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        logger.Error("select文発行で例外エラー。タイトル：" + dataDto.thread + "_" + dataDto.ts + "_" + dataDto.user + " エラー内容：" + ex.Message + " スタックトレース：" + ex.StackTrace);
                    }

                    return false;
                }
            }
        }
    }
}
