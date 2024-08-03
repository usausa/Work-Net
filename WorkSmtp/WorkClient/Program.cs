using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

using var client = new SmtpClient();

using var message = new MimeMessage();

message.From.Add(new MailboxAddress("うさうさ", "usausa@example.com"));
message.To.Add(new MailboxAddress("うさうさ", "usausa@example.com"));
message.Subject = "テスト";
message.Body = new TextPart(TextFormat.Plain)
{
    Text = "メール本文"
};

await client.ConnectAsync("127.0.0.1", 25);

var ret = await client.SendAsync(message);
Console.WriteLine(ret);

await client.DisconnectAsync(true);
