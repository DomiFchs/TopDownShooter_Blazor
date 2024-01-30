namespace TopDownShooter.ApiService.Entities;

public class Pair<T1,T2> where T1 : class where T2 : class {
    public T1? Item1 { get; set; }
    public T2? Item2 { get; set; }
    
    public Pair(T1? item1, T2? item2) {
        Item1 = item1;
        Item2 = item2;
    }
    
    public Pair() {
    }
    
    public static implicit operator Pair<T1,T2>((T1? first, T2? second) pair) {
        return new Pair<T1, T2>(pair.first, pair.second);
    }
    
    public static implicit operator (T1? first, T2? second)(Pair<T1,T2> pair) {
        return (pair.Item1, pair.Item2);
    }
    
    public override string ToString() {
        return $"({Item1}, {Item2})";
    }
    
    public override bool Equals(object? obj) {
        return obj is Pair<T1, T2> pair &&
               EqualityComparer<T1?>.Default.Equals(Item1, pair.Item1) &&
               EqualityComparer<T2?>.Default.Equals(Item2, pair.Item2);
    }
    
    public override int GetHashCode() {
        return HashCode.Combine(Item1, Item2);
    }
    
    public static bool operator ==(Pair<T1, T2> left, Pair<T1, T2> right) {
        return EqualityComparer<Pair<T1, T2>>.Default.Equals(left, right);
    }
    
    public static bool operator !=(Pair<T1, T2> left, Pair<T1, T2> right) {
        return !(left == right);
    }
    
}