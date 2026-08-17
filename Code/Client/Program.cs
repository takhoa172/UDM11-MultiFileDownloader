using Shared;

Console.WriteLine("Nhap password:");
string password = Console.ReadLine() ?? "";

string hash = PasswordHasher.HashPassword(password);

Console.WriteLine("SHA-256:");
Console.WriteLine(hash);
