-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- ����
function talk(luaMgr, client, params)
  kaishitime = luaMgr:GetStartBuChangTime(client);
  jieshutime = luaMgr:GetEndBuChangTime(client);
  buchangexp = luaMgr:GetBuChangExp(client);
  buchangbindyuanbao = luaMgr:GetBuChangBindYuanBao(client);
  goodsnames = luaMgr:GetBuChangGoodsNames(client);
  return '\n'
	 ..'<span class="chuansong"><span class="white">�������쳣�������</span></span>\n'
	 ..'<span class="white">��Ҿ����ڴ˴���ȡ��񲹳���</span>\n\n'

	 ..'<span class="chuansong"><span class="white">��ŭն���з��Ŷ��ڴ˸�л������һ��</span></span>\n'
	 ..'<span class="white">������֧�֣�ף�����Ϸ��죡��ϸ�����Ľ���</span>\n'
	 ..'<span class="white">���£�</span>\n\n'

	 ..'<span class="red">ÿ������ڲ�����ڼ䣬��ȡһ�β�����</span>\n'
	 ..'<span class="white">��ʼʱ�䣺'..kaishitime..'</span>\n'
	 ..'<span class="white">����ʱ�䣺'..jieshutime..'</span>\n\n'

	 ..'<span class="yellow">�������飺</span>'
	 ..'<span class="green">['..buchangexp..']</span>\n'

	 ..'<span class="yellow">������Ԫ����</span>'
	 ..'<span class="white">'..buchangbindyuanbao..'</span>\n'

	 ..'<span class="yellow">������Ʒ��</span>'
	 ..'<span class="white">'..goodsnames..'</span>\n\n'

	 ..'<span class="center"><a href="event:givebuchang">��ȡ����</a></span>\n'

	 
end

function givebuchang(luaMgr, client, params)
     return luaMgr:GiveBuChang(client);
end
  
