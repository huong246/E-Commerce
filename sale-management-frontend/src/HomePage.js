import React, { useState, useEffect } from 'react';
import './HomePage.css'; // Tạo file CSS để style cho đẹp hơn

function HomePage() {
    const [randomItems, setRandomItems] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        // Hàm để gọi API lấy item ngẫu nhiên
        const fetchRandomItems = async () => {
            try {
                // Gọi API đến endpoint mới tạo, ví dụ lấy 10 item
                const response = await fetch('https://localhost:7004/api/Item/list_items_random?count=10');

                if (!response.ok) {
                    throw new Error(`Lỗi HTTP! Status: ${response.status}`);
                }

                const data = await response.json();
                setRandomItems(data); // Lưu dữ liệu vào state
            } catch (e) {
                setError(e.message); // Lưu lỗi vào state
            } finally {
                setLoading(false); // Dừng loading dù thành công hay thất bại
            }
        };

        fetchRandomItems();
    }, []); // Mảng rỗng đảm bảo useEffect chỉ chạy 1 lần

    if (loading) {
        return <div>Đang tải sản phẩm nổi bật...</div>;
    }

    if (error) {
        return <div>Lỗi khi tải dữ liệu: {error}</div>;
    }

    return (
        <div className="homepage">
            <h1>Sản phẩm nổi bật ✨</h1>
            <div className="item-grid">
                {randomItems.length > 0 ? (
                    randomItems.map(item => (
                        <div key={item.id} className="item-card">
                            {/* Giả sử item có thuộc tính imageUrl, nếu không có bạn có thể dùng ảnh mặc định */}
                            <img src={item.imageUrl || 'https://via.placeholder.com/150'} alt={item.name} className="item-image" />
                            <div className="item-info">
                                <h3 className="item-name">{item.name}</h3>
                                {/* Giả sử item có thuộc tính price, định dạng lại cho đẹp */}
                                <p className="item-price">{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(item.price)}</p>
                            </div>
                        </div>
                    ))
                ) : (
                    <p>Chưa có sản phẩm nào để hiển thị.</p>
                )}
            </div>
        </div>
    );
}

export default HomePage;