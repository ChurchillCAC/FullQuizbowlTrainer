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
        void EditButton_Clicked(object sender, EventArgs args)
        {
            SelectPreferencesViewModel.PushCategoriesModal((SelectPreferencesViewModel)this.BindingContext, this.Navigation, 1);
        }
        void DeleteButton_Clicked(object sender, EventArgs args)
        {
            SelectPreferencesViewModel.DeletePresetData((SelectPreferencesViewModel)this.BindingContext);
        }

        void Start_Clicked(object sender, EventArgs args)
        {
            SelectPreferencesViewModel.StartQuizzing((SelectPreferencesViewModel)this.BindingContext, this.Navigation, this);
        }
    }
}
