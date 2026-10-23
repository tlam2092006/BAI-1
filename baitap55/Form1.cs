using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace baitap55
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource _cts;
        private int _signalCount = 0;
        private readonly Random _rand = new Random();

        public Form1()
        {
            InitializeComponent();
            // Ban đầu chưa chạy nên khóa nút Dừng lại
            btnStop.Enabled = false;
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            // Reset dữ liệu ban đầu
            _signalCount = 0;
            _cts = new CancellationTokenSource();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Text = "Trạng thái: Đang kết nối...";

            try
            {
                // Khởi chạy vòng lặp ngầm thu thập dữ liệu tự động
                await Task.Run(() => MonitorLoop(_cts.Token), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Bắt lỗi khi người dùng bấm dừng (Task bị hủy an toàn)
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            // Yêu cầu hủy tiến trình chạy ngầm
            _cts?.Cancel();

            // Cập nhật lại trạng thái nút bấm
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            lblStatus.Text = "Trạng thái: Đã ngắt kết nối";
        }

        private async Task MonitorLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Sinh nhiệt độ ngẫu nhiên từ 30 đến 42 độ C
                double temperature = Math.Round(_rand.NextDouble() * (42 - 30) + 30, 1);
                _signalCount++;
                string currentTime = DateTime.Now.ToString("HH:mm:ss");

                // Gọi hàm cập nhật giao diện đảm bảo chuẩn Thread Safety
                UpdateUI(temperature, currentTime, _signalCount);

                // Lặp định kỳ 2 giây/lần
                try
                {
                    await Task.Delay(2000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void UpdateUI(double temp, string time, int count)
        {
            // QUY TẮC THREAD SAFETY: Kiểm tra nếu gọi từ thread phụ thì dùng Invoke chuyển về UI Thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateUI(temp, time, count)));
                return;
            }

            // === KHU VỰC CẬP NHẬT UI AN TOÀN TRÊN LUỒNG CHÍNH ===

            // 1. Hiển thị nhiệt độ lên Label & Đổi màu (Đỏ nếu > 37°C, Xanh nếu bình thường)
            lblTemperature.Text = $"{temp} °C";
            if (temp > 37.0)
            {
                lblTemperature.ForeColor = Color.Red;
            }
            else
            {
                lblTemperature.ForeColor = Color.Green;
            }

            // 2. Cập nhật StatusStrip
            lblStatus.Text = "Trạng thái: Đang giám sát real-time";
            lblCount.Text = $"Số chu kỳ: {count}";

            // 3. Thêm dòng ghi log vào DataGridView
            dgvLogs.Rows.Add(time, temp.ToString());

            // Tự động cuộn bảng xuống dòng mới nhất
            if (dgvLogs.Rows.Count > 0)
            {
                int lastRowIndex = dgvLogs.Rows.Count - 1;
                dgvLogs.FirstDisplayedScrollingRowIndex = lastRowIndex;
            }
        }
    }
}