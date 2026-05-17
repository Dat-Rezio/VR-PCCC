// ══════════════════════════════════════════════════════════════════
// FILE NÀY CHỈ LÀ TÀI LIỆU THAM KHẢO NỘI DUNG CÂU HỎI MẪU
// Dùng để điền vào ScriptableObject asset trong Unity Editor
// ══════════════════════════════════════════════════════════════════
//
// CÁCH TẠO ASSET:
//   1. Chuột phải trong Project window → Create → VR-PCCC → Quiz Question Data
//   2. Đặt tên: QuizData_AllScenarios
//   3. Chọn file → Inspector → điền các câu hỏi theo nội dung bên dưới
//
// ══════════════════════════════════════════════════════════════════

/*
 ═══════════════════════════════════════════════════════════════
  KỊCH BẢN 1 — KIỂM TRA NHIỆT ĐỘ CỬA (DoorSafetySimulation)
  Bài học: Không mở cửa khi nhiệt độ cao, dùng mu bàn tay kiểm tra
 ═══════════════════════════════════════════════════════════════

  [Q1]
  questionText: "Khi phát hiện khói từ dưới cánh cửa, bạn nên mở ngay cửa ra để thoát hiểm."
  correctAnswer: FALSE (Sai)
  explanation: "KHÔNG mở cửa khi có khói! Nhiệt độ và khói độc phía sau có thể rất nguy hiểm. Hãy dùng mu bàn tay chạm nhẹ vào cửa để kiểm tra nhiệt độ trước."
  scenarioTag: Scenario1

  [Q2]
  questionText: "Nên dùng mu bàn tay (không phải lòng bàn tay) để kiểm tra nhiệt độ cánh cửa khi nghi ngờ có lửa phía sau."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng. Dùng mu bàn tay để kiểm tra vì vùng da này nhạy cảm hơn. Nếu dùng lòng bàn tay và cửa quá nóng, bạn có thể bị phỏng và mất khả năng cầm nắm khi thoát hiểm."
  scenarioTag: Scenario1

  [Q3]
  questionText: "Cánh cửa còn ấm (không quá nóng) nghĩa là hoàn toàn an toàn và bạn có thể mở cửa tự do."
  correctAnswer: FALSE (Sai)
  explanation: "SAI. Ngay cả khi cửa ấm, vẫn cần mở cửa từ từ và cẩn thận. Không khí nóng và khói độc có thể ùa vào ngay lập tức. Cúi thấp người khi mở."
  scenarioTag: Scenario1

  [Q4]
  questionText: "Nếu cửa thoát hiểm quá nóng, bạn nên tìm đường thoát khác thay vì mở cửa đó."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng. Cửa nóng nghĩa là phía sau đang cháy dữ dội. Tìm đường thoát hiểm khác, hoặc quay lại phòng, bịt khe cửa và chờ cứu hộ."
  scenarioTag: Scenario1

  [Q5]
  questionText: "Khi mở cửa thoát hiểm, nên đứng thẳng để quan sát nhanh tình hình bên ngoài."
  correctAnswer: FALSE (Sai)
  explanation: "SAI. Luôn cúi thấp khi mở cửa! Khói và khí độc tích tụ ở phía trên. Không khí sạch hơn ở gần sàn nhà."
  scenarioTag: Scenario1

 ═══════════════════════════════════════════════════════════════
  KỊCH BẢN 2 — CHỮA CHÁY BẰNG BÌNH (FirefightingScenarioManager)
  Bài học: Chọn đúng loại bình, khoảng cách, quy trình P-A-S-S
 ═══════════════════════════════════════════════════════════════

  [Q6]
  questionText: "Bình chữa cháy khí CO₂ (màu đen) thích hợp để dập đám cháy thiết bị điện, điện tử."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng. CO₂ không dẫn điện và không để lại chất bẩn, rất phù hợp cho đám cháy điện (Class C) và đám cháy chất lỏng (Class B) như tủ lạnh."
  scenarioTag: Scenario2

  [Q7]
  questionText: "Bình bột ABC (màu đỏ) có thể dùng để dập đám cháy vải, nệm, gỗ (chất rắn hữu cơ)."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng. Bình bột ABC dập được đám cháy loại A (chất rắn), B (chất lỏng) và C (khí/điện). Phù hợp cho đám cháy nệm, vải, gỗ trong phòng ngủ."
  scenarioTag: Scenario2

  [Q8]
  questionText: "Khoảng cách an toàn để sử dụng bình chữa cháy là càng gần đám cháy càng tốt để tăng hiệu quả."
  correctAnswer: FALSE (Sai)
  explanation: "SAI. Khoảng cách lý tưởng là 2–3 mét. Quá gần sẽ nguy hiểm do nhiệt và có thể làm lửa bùng mạnh hơn. Quá xa thì chất chữa cháy không đến nơi hiệu quả."
  scenarioTag: Scenario2

  [Q9]
  questionText: "Quy trình sử dụng bình chữa cháy đúng là: Rút chốt → Hướng vòi vào gốc lửa → Bóp cò → Quét ngang."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng — đây là quy trình P.A.S.S.: Pull (rút chốt), Aim (hướng vòi), Squeeze (bóp cò), Sweep (quét ngang). Luôn nhắm vào GỐC lửa, không phải ngọn lửa."
  scenarioTag: Scenario2

  [Q10]
  questionText: "Bình khí CO₂ để lâu không dùng vẫn luôn đầy và sẵn sàng sử dụng mà không cần bảo trì."
  correctAnswer: FALSE (Sai)
  explanation: "SAI. Bình chữa cháy cần được kiểm tra định kỳ (thường 6 tháng/lần). Bình có thể bị rò rỉ, hết hạn hoặc hỏng van. Đây là lỗi PCCC phổ biến ở nhiều tòa nhà."
  scenarioTag: Scenario2

 ═══════════════════════════════════════════════════════════════
  KỊCH BẢN 3 — THOÁT HIỂM (EscapeSceneManager)
  Bài học: Báo động, thoát bằng thang bộ, không dùng thang máy
 ═══════════════════════════════════════════════════════════════

  [Q11]
  questionText: "Khi tòa nhà xảy ra hỏa hoạn, có thể sử dụng thang máy để di chuyển xuống tầng nhanh hơn."
  correctAnswer: FALSE (Sai)
  explanation: "TUYỆT ĐỐI KHÔNG. Hệ thống điện có thể bị ngắt khiến thang kẹt giữa chừng. Hệ thống gọi thang có thể đưa bạn thẳng vào tầng đang cháy. Luôn dùng thang bộ thoát hiểm."
  scenarioTag: Scenario3

  [Q12]
  questionText: "Khi không tự dập được lửa, việc đầu tiên cần làm là kích hoạt nút báo cháy khẩn cấp để cảnh báo mọi người."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng. Cảnh báo toàn tòa nhà là ưu tiên hàng đầu khi đám cháy vượt tầm kiểm soát. Mỗi giây trễ có thể ảnh hưởng đến nhiều người."
  scenarioTag: Scenario3

  [Q13]
  questionText: "Khi thoát hiểm qua cầu thang bộ đầy khói, nên bịt mũi và đứng thẳng để chạy nhanh hơn."
  correctAnswer: FALSE (Sai)
  explanation: "SAI. Khói tích tụ phía trên, không khí ở gần sàn sạch hơn. Hãy cúi thấp hoặc bò khi di chuyển qua vùng có khói. Dùng khăn ẩm che mũi miệng nếu có thể."
  scenarioTag: Scenario3

  [Q14]
  questionText: "Nếu bị kẹt trong phòng và không thoát được, nên bịt khe dưới cửa bằng khăn/vải để ngăn khói vào."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng. Bịt khe cửa làm chậm đáng kể sự xâm nhập của khói và nhiệt. Sau đó ra cửa sổ ra hiệu cho lực lượng cứu hộ và chờ được cứu."
  scenarioTag: Scenario3

  [Q15]
  questionText: "Khi thoát hiểm thành công ra ngoài, nên quay lại tòa nhà để lấy tài sản quan trọng còn bỏ lại."
  correctAnswer: FALSE (Sai)
  explanation: "TUYỆT ĐỐI KHÔNG. Mạng người quan trọng hơn tài sản. Cứu hỏa chuyên nghiệp có thiết bị bảo hộ để vào tòa nhà cháy. Không bao giờ tự ý quay lại."
  scenarioTag: Scenario3

 ═══════════════════════════════════════════════════════════════
  KỊCH BẢN 4 — KIỂM TRA NGUY CƠ (InspectionScenarioManager)
  Bài học: Nhận biết các nguy cơ cháy nổ trong gia đình
 ═══════════════════════════════════════════════════════════════

  [Q16]
  questionText: "Đặt vật liệu dễ cháy (vải, giấy, xăng) gần bếp gas là một nguy cơ hỏa hoạn nghiêm trọng."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng. Bếp gas sinh ra tia lửa và nhiệt. Vật liệu dễ cháy trong khoảng cách gần có thể bốc cháy bất ngờ. Vùng xung quanh bếp phải luôn được dọn sạch."
  scenarioTag: Scenario4

  [Q17]
  questionText: "Bình gas gia đình nên được đặt đứng, ở nơi thoáng khí và xa nguồn nhiệt để đảm bảo an toàn."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng. Gas nặng hơn không khí, nếu rò rỉ sẽ tích tụ ở chỗ thấp. Nơi thông thoáng giúp khí tản ra ngoài. Nguồn nhiệt có thể gây cháy nổ ngay lập tức."
  scenarioTag: Scenario4

  [Q18]
  questionText: "Ổ điện cắm quá nhiều thiết bị cùng lúc (quá tải) không phải là nguyên nhân gây hỏa hoạn."
  correctAnswer: FALSE (Sai)
  explanation: "SAI. Quá tải điện là một trong những nguyên nhân hàng đầu gây hỏa hoạn nhà ở tại Việt Nam. Dây điện nóng, chập điện dễ gây cháy. Mỗi ổ cắm chỉ nên cắm thiết bị phù hợp công suất."
  scenarioTag: Scenario4

  [Q19]
  questionText: "Để bật lửa, diêm hoặc thiết bị gây lửa trong tầm với của trẻ em là hành vi nguy hiểm và vi phạm an toàn PCCC."
  correctAnswer: TRUE (Đúng)
  explanation: "Đúng. Trẻ em hiếu kỳ với lửa nhưng chưa hiểu nguy hiểm. Các vụ cháy do trẻ em gây ra chiếm tỷ lệ đáng kể. Cất các thiết bị gây lửa ở nơi cao hoặc có khóa."
  scenarioTag: Scenario4

  [Q20]
  questionText: "Bình chữa cháy gia đình không cần đặt ở vị trí cố định vì có thể di chuyển đến nơi cần khi xảy ra cháy."
  correctAnswer: FALSE (Sai)
  explanation: "SAI. Khi cháy xảy ra, bạn không có thời gian tìm kiếm bình. Bình chữa cháy phải được đặt ở vị trí cố định, dễ nhìn thấy, dễ tiếp cận và tất cả thành viên gia đình đều biết vị trí."
  scenarioTag: Scenario4
*/
