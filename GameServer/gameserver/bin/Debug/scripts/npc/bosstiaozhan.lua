-- kevinh - the following lines are part of our standard init
-- require("compat-5.1")
-- BOSS��ս˵��
function talk(luaMgr, client, params)
  lefttimesStr = luaMgr:GetBossFuBenLeftTimeString(client)
  return '<span class="title_center">BOSS��ս˵��</span>\n'
       ..'<span class="title_0">��ͼ�趨��</span>'
       ..'<span class="padding_60">��15����ʼ����BOSS��ս����\n'
       ..'ÿ5��һ��BOSS,���B0SS60��</span>\n\n'

       ..'<span class="title_0">BOSS���䣺</span>'
       ..'<span class="padding_60">��ս������BOSSװ�����ʳ���\n'
       ..'һ�����ʵ���<span class="red">��������</span></span>\n\n'

       ..'<span class="title_0">��ս������</span>'
       ..'<span class="padding_60">��ͨ���ÿ��3����ս����\n'
       ..'��Ա���ÿ��5����ս����</span>\n\n'

       ..'<span class="title_center2">�̳ǿɹ���BOSS��ս��������ս����</span>\n\n'

       ..'<span class="title_0">ʣ�������</span>'
       ..'<span class="padding_60">'..lefttimesStr..'��</span>\n\n'
       ..'<span class="center"><a href="event:_enterbossfuben">����BOSS��ս����</a></span>\n'
end

function _enterbossfuben(luaMgr, client, params)
  luaMgr:EnterBossFuBen(client)
end
