﻿using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;

namespace Roadkill.Core
{
	/// <summary>
	/// Provides a service for group membership lookup for the <see cref="ActiveDirectoryUserManager"/>
	/// </summary>
	public interface IActiveDirectoryService
	{
		IEnumerable<IRoadKillPrincipal> GetMembers(string domainName, string username, string password, string groupName);
	}
}