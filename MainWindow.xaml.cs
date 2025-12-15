using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace SimpleCalc
{
    public partial class MainWindow : Window
    {
        const int MinVal = -999;
        const int MaxVal = 999;
        static readonly Regex _intPattern = new Regex(@"^-?\d+$");

        public MainWindow()
        {
            InitializeComponent();

            CommandBinding pasteBinding = new CommandBinding(ApplicationCommands.Paste, Paste_Executed, Paste_CanExecute);
            this.CommandBindings.Add(pasteBinding);
        }

        private void ButtonCalc_Click(object sender, RoutedEventArgs e)
        {
            ClearMessages();

            string aText = TextBoxA.Text.Trim();
            string bText = TextBoxB.Text.Trim();

            if (string.IsNullOrEmpty(aText) || string.IsNullOrEmpty(bText))
            {
                TextBlockError.Text = "Оба поля должны быть заполнены.";
                return;
            }

            if (!_intPattern.IsMatch(aText) || !_intPattern.IsMatch(bText))
            {
                TextBlockError.Text = "Ввод должен быть целым числом (без пробелов, разделителей).";
                return;
            }

            if (!int.TryParse(aText, out int a) || !int.TryParse(bText, out int b))
            {
                TextBlockError.Text = "Число слишком велико или некорректно.";
                return;
            }

            if (a < MinVal || a > MaxVal || b < MinVal || b > MaxVal)
            {
                TextBlockError.Text = $"Числа должны быть в диапазоне {MinVal}..{MaxVal}.";
                return;
            }

            try
            {
                int result = RadioAdd.IsChecked == true ? checked(a + b) : checked(a - b);
                TextBlockResult.Text = $"Результат: {result}";
            }
            catch (OverflowException)
            {
                TextBlockError.Text = "Произошло переполнение при вычислении.";
            }
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            TextBoxA.Text = "";
            TextBoxB.Text = "";
            RadioAdd.IsChecked = true;
            ClearMessages();
            TextBoxA.Focus();
        }

        private void ClearMessages()
        {
            TextBlockError.Text = "";
            TextBlockResult.Text = "";
        }

        private void Integer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Блокируем пробелы и прочие неподходящие символы
            if (string.IsNullOrWhiteSpace(e.Text))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            string proposed = GetProposedText(tb, e.Text);
            e.Handled = !IsTextValidInteger(proposed);
        }

        // Блокируем нажатие пробела
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private static string GetProposedText(TextBox tb, string input)
        {
            if (tb == null) return input;
            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;
            string text = tb.Text ?? "";
            string newText = text.Substring(0, selStart) + input + text.Substring(selStart + selLen);
            return newText;
        }

        private static bool IsTextValidInteger(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            if (text == "-") return true;
            if (!_intPattern.IsMatch(text)) return false;
            if (!int.TryParse(text, out int v)) return false;
            return v >= MinVal && v <= MaxVal;
        }

        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox tb)
            {
                string clipboard = Clipboard.GetText() ?? "";
                string proposed = GetProposedText(tb, clipboard.Trim());
                e.CanExecute = IsTextValidInteger(proposed);
            }
            else
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox tb)
            {
                string clipboard = Clipboard.GetText() ?? "";
                string proposed = GetProposedText(tb, clipboard.Trim());
                if (!IsTextValidInteger(proposed))
                {
                    return;
                }
            }
            if (e.Source is ICommandSource) return;
            ApplicationCommands.Paste.Execute(null, Keyboard.FocusedElement as IInputElement);
        }
    }
}
