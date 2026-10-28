using System.Net;

namespace SportsCenterAPI.Helpers;

public static class OtpEmailTemplate
{
    public static string Create(string otp, bool resetPassword)
    {
        string title = resetPassword
            ? "Đặt lại mật khẩu"
            : "Xác nhận đăng ký";

        string description = resetPassword
            ? "Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu tài khoản Sports Center của bạn. Nhập mã bên dưới để tiếp tục."
            : "Cảm ơn bạn đã đăng ký Sports Center. Nhập mã bên dưới để xác nhận email và hoàn tất đăng ký.";

        string safeOtp = WebUtility.HtmlEncode(otp);

        // Đen trắng, chỉ dùng bảng và style nội tuyến để hiển thị ổn định trên mọi trình đọc mail.
        return $$"""
        <!DOCTYPE html>
        <html lang="vi">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>{{title}} | Sports Center</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f4f4f4;font-family:Arial,Helvetica,sans-serif;color:#000000;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f4;">
                <tr>
                    <td align="center" style="padding:32px 12px;">
                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0"
                               style="max-width:560px;background-color:#ffffff;border:1px solid #000000;">
                            <tr>
                                <td style="padding:28px 36px;background-color:#000000;">
                                    <div style="font-size:22px;font-weight:bold;letter-spacing:3px;color:#ffffff;">
                                        SPORTS CENTER
                                    </div>
                                </td>
                            </tr>

                            <tr>
                                <td style="padding:36px;">
                                    <div style="font-size:11px;font-weight:bold;letter-spacing:2px;color:#595959;">
                                        BẢO MẬT TÀI KHOẢN
                                    </div>

                                    <h1 style="margin:10px 0 16px;font-size:26px;line-height:34px;font-weight:bold;color:#000000;">
                                        {{title}}
                                    </h1>

                                    <p style="margin:0 0 28px;font-size:15px;line-height:24px;color:#333333;">
                                        Xin chào,<br>
                                        {{description}}
                                    </p>

                                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0"
                                           style="border:2px solid #000000;">
                                        <tr>
                                            <td align="center" style="padding:24px 10px;">
                                                <div style="font-size:11px;font-weight:bold;letter-spacing:2px;color:#595959;">
                                                    MÃ XÁC NHẬN CỦA BẠN
                                                </div>

                                                <div style="margin:14px 0;font-family:Consolas,'Courier New',monospace;font-size:38px;line-height:46px;font-weight:bold;letter-spacing:8px;color:#000000;">
                                                    {{safeOtp}}
                                                </div>

                                                <div style="font-size:13px;line-height:20px;color:#595959;">
                                                    Có hiệu lực trong <strong style="color:#000000;">5 phút</strong>
                                                </div>
                                            </td>
                                        </tr>
                                    </table>

                                    <p style="margin:28px 0 0;font-size:13px;line-height:22px;color:#333333;">
                                        <strong>Không chia sẻ mã này với bất kỳ ai</strong>, kể cả người tự nhận là nhân viên hỗ trợ.
                                    </p>

                                    <p style="margin:12px 0 0;font-size:13px;line-height:22px;color:#595959;">
                                        Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email.
                                        Không có thay đổi nào được thực hiện chỉ bằng việc gửi mã.
                                    </p>
                                </td>
                            </tr>

                            <tr>
                                <td style="padding:18px 36px;border-top:1px solid #d4d4d4;">
                                    <div style="font-size:12px;font-weight:bold;color:#000000;">
                                        Sports Center
                                    </div>
                                    <div style="margin-top:4px;font-size:12px;line-height:18px;color:#737373;">
                                        Email được gửi tự động. Vui lòng không trả lời.
                                    </div>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }
}
