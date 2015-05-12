﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Screens
{
    

    /// <summary>
    /// Interaction logic for ChangeScreen.xaml
    /// </summary>
    public partial class ChangeScreen : Window
    {
        private MainWindow m_mainwindow = null;

        public ChangeScreen(MainWindow parent)
        {
            InitializeComponent();

            m_mainwindow = parent;

            uc_Child.LoadScreen += new EventHandler(Load_Screen);
        }

        protected void Load_Screen(object sender, EventArgs e)
        {
           m_mainwindow.Load_Screen((string)sender);
        }
    }
}
