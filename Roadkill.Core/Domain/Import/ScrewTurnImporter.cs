﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Roadkill.Core.Domain.Managers;
using System.Text.RegularExpressions;

namespace Roadkill.Core
{
	public class ScrewTurnImporter : IWikiImporter
	{
		private string _connectionString;
		public bool ConvertToCreole { get; set; }

		public void ImportFromSql(string connectionString)
		{
			_connectionString = connectionString;
			using (SqlConnection connection = new SqlConnection(_connectionString))
			{
				using (SqlCommand command = connection.CreateCommand())
				{
					connection.Open();
					command.CommandText = "SELECT p.*,pc.[User] as [User],pc.Revision,pc.LastModified FROM Page p " +
											"INNER JOIN PageContent pc ON pc.Page = p.Name " +
											"WHERE pc.Revision = (SELECT MAX(Revision) FROM PageContent WHERE Page=p.Name)";

					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							Page page = new Page();
							page.Title = reader["Name"].ToString();
							page.CreatedBy = reader["User"].ToString();
							page.CreatedOn = (DateTime)reader["CreationDateTime"];
							page.ModifiedBy = reader["User"].ToString();
							page.ModifiedOn = (DateTime)reader["LastModified"];

							string categories = GetCategories(page.Title);
							if (!string.IsNullOrWhiteSpace(categories))
								categories += ";";
							page.Tags = categories;

							Page.Repository.SaveOrUpdate(page);
							AddContent(page);
						}
					}
				}
			}
		}

		private string GetCategories(string page)
		{
			using (SqlConnection connection = new SqlConnection(_connectionString))
			{
				using (SqlCommand command = connection.CreateCommand())
				{
					connection.Open();
					command.CommandText = "SELECT Category from CategoryBinding_v2 WHERE Page=@Page";

					SqlParameter parameter = new SqlParameter();
					parameter.ParameterName = "@Page";
					parameter.SqlDbType = System.Data.SqlDbType.VarChar;
					parameter.Value = page;
					command.Parameters.Add(parameter);

					List<string> categories = new List<string>();
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							categories.Add(reader.GetString(0).Replace(";","-").Replace(" ","-"));
						}
					}

					return string.Join(";", categories);
				}
			}
		}

		private void AddContent(Page parentPage)
		{
			using (SqlConnection connection = new SqlConnection(_connectionString))
			{
				using (SqlCommand command = connection.CreateCommand())
				{
					connection.Open();
					command.CommandText = "SELECT * FROM PageContent_v2 WHERE Page = @Page";
					
					SqlParameter parameter = new SqlParameter();
					parameter.ParameterName = "@Page";
					parameter.SqlDbType = System.Data.SqlDbType.VarChar;
					parameter.Value = parentPage.Title;
					command.Parameters.Add(parameter);

					List<PageContent> categories = new List<PageContent>();
					bool hasContent = false;
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							PageContent content = new PageContent();
							content.EditedBy = reader["Username"].ToString();
							content.EditedOn = (DateTime)reader["DateTime"];
							content.Text = reader["Content"].ToString();
							content.Text = CleanContent(content.Text);
							content.VersionNumber = ((int)reader["Revision"]) + 1;
							content.Page = parentPage;

							PageContent.Repository.SaveOrUpdate(content);
							hasContent = true;
						}
					}

					// For broken content, make sure the page has something
					if (!hasContent)
					{
						PageContent content = new PageContent();
						content.EditedBy = "unknown";
						content.EditedOn = DateTime.Now;
						content.Text = "";
						content.VersionNumber = 1;
						content.Page = parentPage;

						PageContent.Repository.SaveOrUpdate(content);
					}
				}
			}
		}

		private string CleanContent(string text)
		{
			// Screwturn uses "[" for links instead of "[[", so do a crude replace.
			// Needs more coverage for @@ blocks, variables, toc.
			text = text.Replace("[", "[[").Replace("]", "]]").Replace("{BR}", "\n").Replace("{UP}","");

			// Handle nowiki blocks being a little strange
			Regex regex = new Regex("@@(.*?)@@",RegexOptions.Singleline);
			if (regex.IsMatch(text))
			{
				text = regex.Replace(text,"<nowiki>$1</nowiki>");
			}

			return text;
		}
	}
}