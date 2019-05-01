using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaceDayAPI.Models
{
	public class BaseRepository
	{
		protected RaceDayEntities context = new RaceDayEntities();

		/// <summary>
		/// SaveChanges
		/// 
		/// Save changes to the context
		/// </summary>
		/// 
		public void SaveChanges()
		{
			context.SaveChanges();
		}

		/// <summary>
		/// Reset
		/// 
		/// Discards the current context and creates a new one eliminating all objects
		/// </summary>
		/// 
		public void DiscardAndReset()
		{
			context.Dispose();
			context = new RaceDayEntities();
		}

		/// <summary>
		/// MakeStoredProcedureCommand
		/// 
		/// Create a command object using the provided connection
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		/// 
		protected DbCommand MakeStoredProcedureCommand(DbConnection connection, string name)
		{
			DbCommand command = connection.CreateCommand();
			command.CommandType = CommandType.StoredProcedure;
			command.CommandText = name;

			if (connection.State == ConnectionState.Closed)
				connection.Open();

			return command;
		}

		/// <summary>
		/// MakeInParameter
		/// 
		/// Make an input parameter with a set value
		/// </summary>
		/// <param name="command"></param>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="size"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		/// 
		protected DbParameter MakeInParameter(DbCommand command, string name, DbType type, int size, object value)
		{
			DbParameter param = command.CreateParameter();
			param.ParameterName = name;
			param.DbType = type;
			param.Size = size;
			param.Value = value;

			return param;
		}
	}
}
