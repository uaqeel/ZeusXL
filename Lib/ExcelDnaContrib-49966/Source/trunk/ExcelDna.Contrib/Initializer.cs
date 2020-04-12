/*
  Copyright (C) 2009 Hayden Smith

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Hayden Smith
  hayden.smith@gmail.com
*/

using System;
using System.Configuration;
using System.Reflection;
using System.Windows.Forms;
using ExcelDna.Contrib.Library;
using ExcelDna.Integration;

namespace ExcelDna.Contrib
{
    public class Initializer : IExcelAddIn
    {
        public void AutoOpen()
        {
            //try
            //{
            //    string splashPath = Properties.Settings.Default.SplashImage;
            //    if (!string.IsNullOrEmpty(splashPath))
            //    {
            //        splashPath = Utilities.PrependAssemblyPath(splashPath);
            //        SplashScreen.Display(splashPath, false);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.ToString());
            //    throw;
            //}
        }

        public void AutoClose()
        {
            //MessageBox.Show("Now in AutoClose.");
        }
    }
}
