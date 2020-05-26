-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- ��������
function talk(luaMgr, client, params)
-- �������������ʱ��
  result, nearTime, nearReqBangHui = luaMgr:GetNextCityBattleTimeAndBangHui()
  return '<span class="title_center">�������Ի˵��</span>\n'
	 ..'<span class="title_0">��ͼ�趨��</span>'
	 ..'<span class="padding_60">���Ǽ��ʹ���ͼ����ԭ�ظ����\n'
	 ..'ʹ�á��������ʯ��\n'
	 ..'<span class="mi">�����������Ʒ,����ע�⣡</span></span>\n\n'

	 ..'<span class="title_0">ռ�����ǣ�</span>'
	 ..'<span class="padding_60">����ս�۽�����,�ʹ���ֻʣһ��\n'
	 ..'�л�ĳ�Ա\n'
	 ..'<span class="mi">�����ʱ����,��ʱ�ɶ����Ƿ�\n�𹥻�</span></span>\n\n'

	 ..'<span class="title_0">����˵����</span>'
	 ..'<span class="padding_60">���������ÿ�տ������ȡ\n'
	 ..'<span class="mi">Ԫ��*2000��ͭǮ*100��</span></span>\n\n'

	 ..'<span class="white">������Я�����ǺŽǡ�����������������ս\n'
	 ..'���ǺŽǣ��ɻ�ɱ������ħ�������\n\n</span>'

	 ..'<span class="center"><a href="event:_requestcitywar">������������</a></span>\n'
	 ..'<span class="white">���ʱ�䣺��'..nearTime..'��\n'
	 ..'�ɰ�᣺��'..nearReqBangHui..'�����봥������</span>\n\n'
  
	 ..'<span class="center"><a href="event:_lookQuanBuZhanYi">�鿴ȫ��ս��</a></span>\n'
end

-- ������������===_getcityaward������ȡ������һ���ɿͻ����Լ�����ָ�����
function _requestcitywar(luaMgr, client, params)
--    ���ֻ��Ҫдһ���պ������У���дҲ�У���������ᱻ�ͻ��˽�󣬲��ɿͻ��˷�������ָ����ɣ���Ϊ���������һ����ָ��֧��
end

-- �鿴ȫ��ս��
function _lookQuanBuZhanYi(luaMgr, client, params)
  local sItems = luaMgr:GetCityBattleTimeAndBangHuiListString()
  local list = Split(sItems, ',', 20)
  local myStr = '<font size="12" color="#eefe2c">������������ʱ��:\n\n'

  --��װ��ʾ
  for i,s in ipairs(list) do
    if i % 2 > 0 then
	myStr = myStr..'<font color="#ff0000">'..s..'\n'
    else
	myStr = myStr..'<font color="#00ff00">�ɰ��['..s..']���봥������\n'
    end
  end

  myStr = myStr..'\n<font size="12" color="#00ff00"><a class="myunderline" href="event:_lookPrePage">����</a>\n\n'

  return myStr
end
 
-- ����
function _lookPrePage(luaMgr, client, params)
  return talk(luaMgr, client, params)
end

-- �ַ����з�
function Split(str, delim, maxNb)    
    -- Eliminate bad cases...    
    if string.find(str, delim) == nil then   
        return { str }   
    end   
    if maxNb == nil or maxNb < 1 then   
        maxNb = 0    -- No limit    
    end   
    local result = {}   
    local pat = "(.-)" .. delim .. "()"    
    local nb = 0   
    local lastPos    
    for part, pos in string.gfind(str, pat) do   
        nb = nb + 1   
        result[nb] = part    
        lastPos = pos    
        if nb == maxNb then break end   
    end   
    -- Handle the last field    
    if nb ~= maxNb then   
        result[nb + 1] = string.sub(str, lastPos)    
    end   
    return result    
end  
