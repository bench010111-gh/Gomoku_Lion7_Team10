//오목알색 열거형
public enum StoneType
{
    Empty = 0,
    Black = 1,
    White = 2
}

//패턴 열거형
public enum LinePattern
{
    None,
    OpenThree,  //열린 3 (_ o o o _)
    Four,       //4 (_ o o o o _ / x o o o o _)
    Overline    //6목 이상 (o o o _ o o o / o o o o _ o o o)
}

//착수시 금수, 범위 이탈, 자리 있음, 성공 열거형
public enum PlaceResult
{
    Success,        //착수성공
    OutOfBounds,    //오목판 범위 이탈
    AlreadyPlaced,  //이미 돌이 있음
    Forbidden       //금수 자리
}