using TMPro;
using UnityEngine;

/// <summary>
/// TMP Dynamic SDF 폰트 아틀라스에 한글 글리프를 사전 로드한다.
/// 한 프레임에 다량의 한글 문자가 요청되면 아틀라스 공간 부족으로
/// 일부 글자가 렌더링되지 않는 문제를 방지한다.
/// </summary>
public static class SrpFontWarmup
{
    static bool _done;

    public static void Warmup()
    {
        if (_done) return;
        _done = true;

        var font = TMP_Settings.defaultFontAsset;
        if (font == null) return;

        const string uiChars =
            "가각간갈감갑강개객거건검격견결경계고곤골공과관광괴교구국군굴권귀규균극근글금급기긴길김깅" +
            "나남내너널네년노논뇌능니닉닌닐님다단달담답당대더덜도독돌동두둔드득들디딘딜때또라란랑래략량" +
            "러럭런렁레력련렬령례로록론롤료루룬류률르른를리력린릴림립마막만말망매맥면멸명모목몰무문물미민" +
            "밀바박반발밝방배백번벌범법변별병보복본볼부북분불비빈빙사삭산살상새색생서석선설성세소속손수숙" +
            "순술스슬습승시식신실심십아악안알암압앙애액야약양어억언얼업에여역연열영예오온올완왜외요용우운" +
            "원월위유육은을음응의이인일임입자작잔장재저적전절점접정제조족존종좌주준중즉증지직진질집징차착찬" +
            "참창처천철체초총최추축출충취측치칭카커컨켓코콘크큰클킬타탈터턴텍토통투특틸파판패편평폐포폭표" +
            "품프피필하학한할함합항해핵행허험현혈형혼확환활황회획횟효후훈휴흐흑흘흥히힘" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "abcdefghijklmnopqrstuvwxyz" +
            "0123456789" +
            "[](){}×+−=<>:;,.!?/|@#%&*\"'← →↑↓◀▶";

        font.TryAddCharacters(uiChars);
    }
}
