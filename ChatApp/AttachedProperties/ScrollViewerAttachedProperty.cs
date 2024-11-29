using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChatApp.AttachedProperties
{
    public class ScrollViewerAttachedProperty
    {
        // Đăng ký thuộc tính đính kèm AutoScroll
        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached(
                "AutoScroll",
                typeof(bool),
                typeof(ScrollViewerAttachedProperty),
                new PropertyMetadata(false, AutoScrollPropertyChanged));

        // Getter cho thuộc tính AutoScroll
        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        // Setter cho thuộc tính AutoScroll
        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

        // Phương thức xử lý khi giá trị thuộc tính AutoScroll thay đổi
        private static void AutoScrollPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is ScrollViewer scrollViewer)
            {
                if ((bool)args.NewValue) // Nếu AutoScroll được đặt thành true
                {
                    scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged; // Thêm sự kiện ScrollChanged
                    scrollViewer.ScrollToEnd(); // Cuộn tới cuối khi kích hoạt AutoScroll
                }
                else // Nếu AutoScroll được đặt thành false
                {
                    scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged; // Gỡ bỏ sự kiện ScrollChanged
                }
            }
        }

        // Sự kiện ScrollChanged sẽ cuộn xuống cuối khi nội dung thay đổi
        private static void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0) // Kiểm tra nếu chiều cao của nội dung thay đổi
            {
                var scrollViewer = sender as ScrollViewer;
                scrollViewer?.ScrollToBottom(); // Cuộn xuống cuối cùng
            }
        }
    }
}
