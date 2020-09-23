﻿using System;
using System.Collections.Generic;
using FullQuizbowlTrainer.Models;
using FullQuizbowlTrainer.ViewModels;
using Xamarin.Forms;

namespace FullQuizbowlTrainer.Views
{
    public partial class SelectPreferences : ContentPage
    {
        public SelectPreferences(List<Categories> CategoryDat)
        {
            InitializeComponent();
            this.BindingContext = new SelectPreferencesViewModel(CategoryDat);
        }

        void AddButton_Clicked(object sender, EventArgs args)
        {
            SelectPreferencesViewModel.PushCategoriesModal((SelectPreferencesViewModel)this.BindingContext, this.Navigation, 0);
        }
    }
}
