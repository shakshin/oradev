using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace oradev
{
    public class DBObject : DependencyObject
    {
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Name.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(DBObject), new PropertyMetadata(string.Empty));




        public string Type
        {
            get { return (string)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Type.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(string), typeof(DBObject), new PropertyMetadata(string.Empty));




        public bool IsInvalid
        {
            get { return (bool)GetValue(IsInvalidProperty); }
            set { SetValue(IsInvalidProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsInvalid.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsInvalidProperty =
            DependencyProperty.Register("IsInvalid", typeof(bool), typeof(DBObject), new PropertyMetadata(false));




        public bool IsInvalidBody
        {
            get { return (bool)GetValue(IsInvalidBodyProperty); }
            set { SetValue(IsInvalidBodyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsInvalidBody.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsInvalidBodyProperty =
            DependencyProperty.Register("IsInvalidBody", typeof(bool), typeof(DBObject), new PropertyMetadata(false));






        public bool IsInvalidHead
        {
            get { return (bool)GetValue(IsInvalidHeadProperty); }
            set { SetValue(IsInvalidHeadProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsInvalidHead.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsInvalidHeadProperty =
            DependencyProperty.Register("IsInvalidHead", typeof(bool), typeof(DBObject), new PropertyMetadata(false));




        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Description.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(DBObject), new PropertyMetadata(string.Empty));



    }

    
}
