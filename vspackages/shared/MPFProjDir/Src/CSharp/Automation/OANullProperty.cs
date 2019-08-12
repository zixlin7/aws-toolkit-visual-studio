/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Project.Automation
{
	/// <summary>
	/// This object defines a so called null object that is returned as instead of null. This is because callers in VSCore usually crash if a null propery is returned for them.
	/// </summary>
	[CLSCompliant(false), ComVisible(true)]
	public class OANullProperty : EnvDTE.Property
	{
		#region fields
		private OAProperties parent;
		#endregion

		#region ctors

		public OANullProperty(OAProperties parent)
		{
			this.parent = parent;
		}
		#endregion

		#region EnvDTE.Property

		public object Application => String.Empty;

        //todo: EnvDTE.Property.Collection
        public EnvDTE.Properties Collection => this.parent;

        public EnvDTE.DTE DTE => null;

        public object get_IndexedValue(object index1, object index2, object index3, object index4)
		{
			return String.Empty;
		}

		public void let_Value(object value)
		{
			//todo: let_Value
		}

		public string Name => String.Empty;

        public short NumIndices => 0;

        public object Object
		{
			get => this.parent.Target;
            set
			{
			}
		}

		public EnvDTE.Properties Parent => this.parent;

        public void set_IndexedValue(object index1, object index2, object index3, object index4, object value)
		{

		}

		public object Value
		{
			get => String.Empty;
            set { }
		}
		#endregion
	}
}
