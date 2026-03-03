# keyviewer
여기 아무거나 적어서 푸시해보삼 : jjj1
 테스트좀

1. Form1.cs 메인파일(Main Controller)
	(1)HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	- 윈도우의 키보드의 모든 신호(메시지)를 가장 먼저 받는 입구, WM_KEYDOWM/UP을 구분해 신호를 분류_
	
	(2)HandleGlobalKeyDown(Keys key)
	- 키 신호를 화면에 있는 모든 자판(KeyPanel)들에게 뿌림
	
	(3)Panel_DrawKey(object? sender, PaintEventArgs e)
	- 이벤트 발생 시 호출되는 시각화 함수, DrawString 함수로 자판의 중앙에 키 이름을 시각화하며 배경색에 따른 가독성을 위해 GetContrastColor 로직을 사용

2. KeyPanel.cs 자판 하나하나를 나타내는 클래스(Data Model)
	(1)HandleKeyDown(Keys key)
	- 키보드 신호가 들어오면, 이 자판이 눌린 키와 일치하는지 확인하고, 일치하면 _isPressed를 true로 바꿔서 자판이 눌린 상태로 보이게 함.
	
	(2)OnPaint(PaintEventArgs e)
	- 자판의 시각적 표현을 담당하는 함수, _isPressed 상태에 따라 자판의 색상을 바꿔서 눌린 상태와 안 눌린 상태를 구분해서 보여줌.
	
	(3)HandleKeyUp/Down(Keys key)
	-키에서 손을 떼었을 때 색을 원래대로 돌려놓음.(색으로 활성화 비활성화 구분)

3. KeyPanelService.cs 자판 생성 및 드래그 관리
	(1)CreateKeyPanels(Form form)
	- 메인 폼에 자판들을 생성해서 추가하는 함수, QWERTY 배열에 따라 KeyPanel 객체들을 만들어서 폼에 배치함.
	
	(2)KeyPanel_MouseDown(object? sender, MouseEventArgs e)
	- 자판을 클릭했을 때 드래그를 시작하는 이벤트 핸들러, 클릭한 자판을 _draggedPanel로 저장하고 드래그 상태로 전환함.
	
	(3)KeyPanel_MouseMove(object? sender, MouseEventArgs e)
	- 마우스를 드래그하는 동안 드래그 중인 자판의 위치를 업데이트하는 이벤트 핸들러. 마우스 이동 거리를 계산해 자판의 Location 좌표를 실시간 업데이트함.
	
	(4)KeyPanel AddKeyPanel(Keys key, Color downColor, Color upColor, Point location, Size size)
	- 새로운 KeyPanel(자판)을 생성하고 폼에 등록하는 함수.

	(5)ApplyContextAndHandlersRecursive
	-생성된 자판에 우클릭 메뉴와 마우스 핸들러를 재귀적으로 연결하여 확장성을 확보함.

4. LayoutManager.cs 자판의 위치와 크기를 관리하는 클래스
	(1)ArrangeKeyPanels(List<KeyPanel> keyPanels, Size formSize)
	- 자판들을 폼의 크기에 맞게 배치하는 함수, QWERTY 배열에 따라 자판들의 위치를 계산해서 설정함.
	
	(2)Point CalculateKeyPosition(int index, Size formSize)
	- 각 자판의 인덱스와 폼 크기를 기반으로 자판의 위치를 계산하는 함수, QWERTY 배열과 폼 크기에 따라 자판의 위치를 결정함.

	(3)string json = JsonSerializer.Serialize(layout, _jsonOptions);
	- 자판의 좌표(X, Y)와 크기(Width, Height)와 색상 정보를 JSON 형식으로 직렬화해서 파일로 저장하는 부분, Layout 객체를 JSON 문자열로 변환함.)
	
	(4)KeyLayout CreateSampleLayout(string name)
	- 처음에 아무 설정이 없을 경우 'A', 'S', 'D', 'W' 자판을 기본적으로 만들어주는 코드

	(5)SaveLayout / LoadLayout
	-사용자의 커스텀 배치와 설정을 파일로 영구 저장 및 로드
	
	(6)ApplyLayout(List<KeyPanel> keyPanels, KeyLayout layout)
	-JSON데이터를 순회하며 KeyPanelService를 통해 화면에 자판들을 재구성함.

5. PanelEditorControl.cs 자판을 우클릭해서 '편집'을 눌렀을 때 뜨는 수정 창의 로직
	(1)BtnUpColor_Click(object? sender, EventArgs e), private void BtnDownColor_Click(object? sender, EventArgs e)
	-ColorDialog를 띄워서 사용자가 원하는 색상을 직접 고르게 하고, 그 결과를 프리뷰 박스에 보여줌.