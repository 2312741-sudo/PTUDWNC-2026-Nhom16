import type { Metadata } from 'next';
import './globals.css';
import Header from '@/components/Header';
import Footer from '@/components/Footer';

export const metadata: Metadata = {
  title: 'Culinary Blog — Khám phá & Chia sẻ công thức ẩm thực đỉnh cao',
  description:
    'Nền tảng chia sẻ công thức nấu ăn, mẹo nhà bếp và hương vị ẩm thực Việt Nam & Thế giới. Tìm kiếm công thức chuẩn vị, dinh dưỡng và chi tiết từng bước.',
  keywords: ['ẩm thực', 'công thức nấu ăn', 'nấu ăn', 'món ngon mỗi ngày', 'culinary blog'],
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi">
      <body className="antialiased flex flex-col min-h-screen">
        <Header />
        <main className="flex-1">{children}</main>
        <Footer />
      </body>
    </html>
  );
}
