-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- ����
function talk(luaMgr, client, params)
  kaohuoJiuType = luaMgr:GetErGuoTouBufferName(client);
  kaohuoShengyuTime = luaMgr:GetErGuoTouBufferLeftTime(client);
  kaohuoShouyi = luaMgr:GetErGuoTouBufferExperience(client);
  kaohuoShengyuNum = luaMgr:GetErGuoTouTodayLeftUseTimes(client);
  return '\n'
	 ..'<span class="chuansong"><span class="white">��������ȫ�쿪�ţ�ÿ����һƿ</span><span class="green">[����ͷ]</span></span>\n'
	 ..'<span class="white">���Ի��</span><span class="green">30����</span><span class="white">����ʱ�䣡</span>\n\n'

	 ..'<span class="chuansong"><span class="white">�Ⱦƺ�ÿ</span><span class="green">1����</span><span class="white">���һ�ξ������棬���õ�</span></span>\n'
	 ..'<span class="green">[����ͷ]���Խ��</span><span class="white">����õľ�������Խ�ߣ�</span>\n\n'

	 ..'<span class="chuansong"><span class="green">[����ͷ]</span><span class="white">���ڰ�Ԫ���̳ǹ���</span></span>\n\n'

	 ..'<span class="chuansong"><span class="red">ֻ�������ǰ�ȫ��������ܻ������</span></span>\n\n'	

	 ..'<span class="yellow">��ǰ�������ͣ�</span>'
	 ..'<span class="green">['..kaohuoJiuType..']</span>\n'

	 ..'<span class="yellow">ʣ�࿾��ʱ�䣺</span>'
	 ..'<span class="white">'..kaohuoShengyuTime..'</span>\n'

	 ..'<span class="yellow">ÿһ�������棺</span>'
	 ..'<span class="white">'..kaohuoShouyi..'</span>\n'

	 ..'<span class="yellow">���ջ������ƣ�</span>'
	 ..'<span class="white">'..kaohuoShengyuNum..'ƿ</span>\n\n'

	 ..'<span class="yellow">��ѡ��Ҫ�ϳɵľƣ�</span>\n'
	 ..'<a href="event:_mergewin!401">[����ͷ(5��)]��</a><span class="white">�ɻ��2��������</span>\n'
	 ..'<a href="event:_mergewin!401">[����ͷ(10��)]��</a><span class="white">�ɻ��4��������</span>\n'
	 ..'<a href="event:_mergewin!401">[����ͷ(20��)]��</a><span class="white">�ɻ��8��������</span>\n'

	 
end
  
