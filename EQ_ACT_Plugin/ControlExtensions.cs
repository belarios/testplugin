// EQ_ACT_Plugin ~ ControlExtensions.cs
// 
// Copyright © 2017 Ravahn - All Rights Reserved
// 
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
using System;
using System.Windows.Forms;
using System.Collections.Generic;

public static class ControlExtensions
{
    /// <summary>
    /// Executes the Action on the UI thread.  Blocks calling thread.
    /// </summary>
    public static void UIThread(this Control @this, Action code)
    {
        if (@this.InvokeRequired)
        {
            if (@this.IsDisposed || @this.Disposing)
                return;
            @this.Invoke(code);
        }
        else
        {
            code.Invoke();
        }
    }
}