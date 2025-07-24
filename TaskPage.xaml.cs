using System.Windows;
using System.Windows.Controls;

namespace desktop_assistant
{
    public partial class TaskPage : UserControl
    {
        public bool HasFocus => TaskInput.IsKeyboardFocusWithin;

        public TaskPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// タスクを追加
        /// </summary>
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string taskText = TaskInput.Text.Trim();
            if (!string.IsNullOrEmpty(taskText))
            {
                TaskList.Items.Add(taskText);
                TaskInput.Clear();
            }
        }
    }
}
