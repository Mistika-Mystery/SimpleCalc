using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace SimpleCalc
{
    public partial class MainWindow : Window
    {
        // Константы с допустимыми значениями для ввода
        const int MinVal = -999;
        const int MaxVal = 999;

        // Регулярное выражение для проверки, что строка — целое число с возможным знаком минус
        static readonly Regex _intPattern = new Regex(@"^-?\d+$");

        // Конструктор окна — выполняется при создании окна
        public MainWindow()
        {
            InitializeComponent(); // Загружает XAML-разметку и связывает элементы управления с полями класса

            // Создаём обработку команды вставки (Paste) для всего окна.
            // Это нужно, чтобы контролировать вставляемый текст и не допустить пробелов или некорректных чисел.
            CommandBinding pasteBinding = new CommandBinding(ApplicationCommands.Paste, Paste_Executed, Paste_CanExecute);
            this.CommandBindings.Add(pasteBinding);
        }

        // Нажатие кнопки "Вычислить"
        private void ButtonCalc_Click(object sender, RoutedEventArgs e)
        {
            ClearMessages(); // Сначала убираем старые сообщения об ошибках и предыдущий результат

            // Берём текст из полей ввода и убираем пробелы в начале/в конце
            string aText = TextBoxA.Text.Trim();
            string bText = TextBoxB.Text.Trim();

            // Проверяем, что оба поля заполнены — это простая и понятная валидация
            if (string.IsNullOrEmpty(aText) || string.IsNullOrEmpty(bText))
            {
                TextBlockError.Text = "Оба поля должны быть заполнены.";
                return; // Прерываем выполнение: дальше вычислять нечего
            }

            // Проверяем формат: должны быть только цифры, возможно с ведущим минусом
            // Регулярное выражение _intPattern обеспечивает это.
            if (!_intPattern.IsMatch(aText) || !_intPattern.IsMatch(bText))
            {
                TextBlockError.Text = "Ввод должен быть целым числом (без пробелов, разделителей).";
                return;
            }

            // Пробуем преобразовать строки в int. TryParse безопасно возвращает false, если парсинг не удался.
            if (!int.TryParse(aText, out int a) || !int.TryParse(bText, out int b))
            {
                TextBlockError.Text = "Число слишком велико или некорректно.";
                return;
            }

            // Проверяем, что числа находятся в разрешённом диапазоне
            if (a < MinVal || a > MaxVal || b < MinVal || b > MaxVal)
            {
                TextBlockError.Text = $"Числа должны быть в диапазоне {MinVal}..{MaxVal}.";
                return;
            }

            // Выполняем вычисление. checked используется, чтобы поймать переполнение (на всякий случай, хотя в данном случае это не обязательно).
            try
            {
                int result = RadioAdd.IsChecked == true ? checked(a + b) : checked(a - b);
                TextBlockResult.Text = $"Результат: {result}";
            }
            catch (OverflowException)
            {
                // В обычном диапазоне -999..999 переполнение не произойдёт, но тут наглядная защита.
                TextBlockError.Text = "Произошло переполнение при вычислении.";
            }
        }

        // Нажатие кнопки "Очистить" — сбрасываем все поля и сообщения
        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            TextBoxA.Text = "";
            TextBoxB.Text = "";
            RadioAdd.IsChecked = true; // возвращаем опцию по умолчанию
            ClearMessages();
            TextBoxA.Focus(); // ставим фокус в первое поле, чтобы удобнее вводить дальше
        }

        // Помощник для очистки сообщений об ошибке и результата
        private void ClearMessages()
        {
            TextBlockError.Text = "";
            TextBlockResult.Text = "";

        }

        // Этот обработчик срабатывает при вводе текста в TextBox (каждый вводимый символ)
        private void Integer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Блокируем пробелы и другие символы, состоящие только из пробельных символов
            if (string.IsNullOrWhiteSpace(e.Text))
            {
                e.Handled = true; // true — означает: отменяем ввод этого символа
                return;
            }

            var tb = sender as TextBox;
            // Получаем текст, какой бы получился после ввода (это нужно, чтобы запретить ввод минуса не в начале и т.д.)
            string proposed = GetProposedText(tb, e.Text);
            // Если предложенный текст не соответствует требованиям — отменяем ввод
            e.Handled = !IsTextValidInteger(proposed);
        }

        // Этот обработчик блокирует нажатие пробела с клавиатуры (Key.Space)
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // отменяем нажатие пробела
            }
        }

        // Формирует строку, которая получится в TextBox, если вставить/ввести текст в текущую позицию.
        // Нужна для проверки корректности результата до фактического изменения текста в контроле.
        public static string GetProposedText(TextBox tb, string input)
        {
            if (tb == null) return input;
            int selStart = tb.SelectionStart;    // позиция курсора
            int selLen = tb.SelectionLength;     // выделение (если выделен текст, он будет заменён)
            string text = tb.Text ?? "";
            string newText = text.Substring(0, selStart) + input + text.Substring(selStart + selLen);
            return newText;
        }

        // Проверяет, является ли строка допустимым целым числом в требуемом диапазоне
        public static bool IsTextValidInteger(string text)
        {
            if (string.IsNullOrEmpty(text)) return true; // разрешаем пустую строку (пользователь может чистить поле)
            if (text == "-") return true; // разрешаем только минус (пользователь может вввести минус перед цифрами)
            if (!_intPattern.IsMatch(text)) return false; // если не похожа на целое число — невалидна
            if (!int.TryParse(text, out int v)) return false; // дополнительно пытаемся распарсить
            return v >= MinVal && v <= MaxVal; // проверяем диапазон
        }

        // Этот метод вызывается, чтобы решить, можно ли выполнять команду Paste (вставку)
        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox tb)
            {
                // Берём текст из буфера обмена и проверяем, станет ли поле валидным после вставки
                string clipboard = Clipboard.GetText() ?? "";
                string proposed = GetProposedText(tb, clipboard.Trim()); // trim убирает случайные пробелы в начале/конце
                e.CanExecute = IsTextValidInteger(proposed); // если валидно — разрешаем вставку
            }
            else
            {
                e.CanExecute = true; // если фокус не в TextBox — оставляем стандартное поведение
            }
            e.Handled = true; // говорим, что событие обработано
        }

        // Обработка самой команды Paste (вставки)
        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox tb)
            {
                string clipboard = Clipboard.GetText() ?? "";
                string proposed = GetProposedText(tb, clipboard.Trim());
                if (!IsTextValidInteger(proposed))
                {
                    // Если после вставки поле станет невалидным — просто не выполняем вставку
                    return;
                }
            }
            // Здесь выполняется стандартная вставка для текущего элемента с фокусом
            if (e.Source is ICommandSource) return;
            ApplicationCommands.Paste.Execute(null, Keyboard.FocusedElement as IInputElement);
        }
    }
}


//пройденые тесты