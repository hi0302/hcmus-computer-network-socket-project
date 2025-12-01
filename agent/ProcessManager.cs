using System;
using System.Diagnostics;
using System.Text;

namespace ClientAgent.Features
{
    public static class ProcessManager
    {
        /// <summary>
        /// Lấy danh sách toàn bộ tiến trình đang chạy
        /// Định dạng trả về: PID|Tên_Process|Tiêu_đề_cửa_sổ|Ram_SD(MB)<Dòng_mới>
        /// </summary>
        public static string GetProcesses()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                Process[] processList = Process.GetProcesses();

                foreach (Process p in processList)
                {
                    try
                    {
                        // Lấy các thông tin cơ bản
                        int pid = p.Id;
                        string name = p.ProcessName;
                        string title = p.MainWindowTitle; // Tiêu đề cửa sổ (nếu có)

                        // Lấy dung lượng RAM đang dùng (đổi ra MB)
                        long memUsage = p.WorkingSet64 / 1024 / 1024;

                        // Ghép chuỗi, dùng ký tự đặc biệt để phân cách (ví dụ |||)
                        // Format: PID|||Name|||Title|||Memory
                        sb.Append($"{pid}|||{name}|||{title}|||{memUsage}MB");
                        sb.Append(Environment.NewLine); // Xuống dòng cho process tiếp theo
                    }
                    catch
                    {
                        // Bỏ qua nếu không đọc được thông tin process (do quyền hạn)
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                return "ERROR_GET_PROC|" + ex.Message;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Hủy (Kill) một tiến trình dựa trên PID
        /// </summary>
        public static string KillProcess(int pid)
        {
            try
            {
                Process p = Process.GetProcessById(pid);
                p.Kill();
                return $"SUCCESS|Đã kill process ID {pid}";
            }
            catch (ArgumentException)
            {
                return $"ERROR|Process ID {pid} không tồn tại.";
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return $"ERROR|Không đủ quyền để kill process ID {pid} (Cần Run as Admin).";
            }
            catch (Exception ex)
            {
                return $"ERROR|Lỗi: {ex.Message}";
            }
        }

        /// <summary>
        /// Khởi động một tiến trình mới (Start App)
        /// </summary>
        public static string StartProcess(string path)
        {
            try
            {
                Process.Start(path);
                return $"SUCCESS|Đã khởi chạy: {path}";
            }
            catch (Exception ex)
            {
                return $"ERROR|Không thể chạy: {ex.Message}";
            }
        }
    }
}