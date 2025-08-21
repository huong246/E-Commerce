import React from 'react';
import './App.css';
import HomePage from "./HomePage"; // Chỉ cần import HomePage

function App() {
  // Không cần state hay useEffect ở đây nữa

  return (
      <div className="App">
        {/* Bạn có thể thêm các component khác như Navbar (thanh điều hướng) ở trên HomePage */}

        <HomePage /> {/* Chỉ cần hiển thị HomePage là đủ */}

        {/* Hoặc Footer (chân trang) ở dưới */}
      </div>
  );
}

export default App;