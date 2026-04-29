//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System;
using CodeBrix.AssemblyTools.Collections.Generic;

namespace CodeBrix.AssemblyTools; //was previously: Mono;

static class Empty<T> {

	public static readonly T [] Array = new T [0];
}

class ArgumentNullOrEmptyException : ArgumentException {

	public ArgumentNullOrEmptyException (string paramName)
		: base ("Argument null or empty", paramName)
	{
	}
}
