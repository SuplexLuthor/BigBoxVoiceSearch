using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Markup;

namespace BigBoxVoiceSearchConverters
{
    public class DivideByConverter : IValueConverter
    {
        public double A { get; set; }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double a = GetDoubleValue(parameter, A);

            double x = GetDoubleValue(value, 0.0);

            return (x / a);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double a = GetDoubleValue(parameter, A);

            double y = GetDoubleValue(value, 0.0);

            return (a / y);
        }

        #endregion


        private double GetDoubleValue(object parameter, double defaultValue)
        {
            double a;
            if (parameter != null)
                try
                {
                    a = System.Convert.ToDouble(parameter);
                }
                catch
                {
                    a = defaultValue;
                }
            else
                a = defaultValue;
            return a;
        }
    }


    public class GreaterThan : MarkupExtension, IValueConverter
    {
        //  The only public constructor is one that requires a double argument.
        //  Because of that, the XAML editor will put a blue squiggly on it if 
        //  the argument is missing in the XAML. 
        public GreaterThan(double opnd)
        {
            Operand = opnd;
        }

        /// <summary>
        /// Converter returns true if value is greater than this.
        /// 
        /// Don't let this be public, because it's required to be initialized 
        /// via the constructor. 
        /// </summary>
        protected double Operand { get; set; }

        //  When the XAML is parsed, each markup extension is instantiated 
        //  and the parser asks it to provide its value. Here, the value is 
        //  us. 
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) > Operand;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LessThan : MarkupExtension, IValueConverter
    {
        //  The only public constructor is one that requires a double argument.
        //  Because of that, the XAML editor will put a blue squiggly on it if 
        //  the argument is missing in the XAML. 
        public LessThan(double opnd)
        {
            Operand = opnd;
        }

        /// <summary>
        /// Converter returns true if value is greater than this.
        /// 
        /// Don't let this be public, because it's required to be initialized 
        /// via the constructor. 
        /// </summary>
        protected double Operand { get; set; }

        //  When the XAML is parsed, each markup extension is instantiated 
        //  and the parser asks it to provide its value. Here, the value is 
        //  us. 
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) < Operand;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
