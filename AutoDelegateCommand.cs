using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;

namespace TestModule
{
  /*
   Similar to DelegateCommand that is commonly used for WPF but it automatically fires CanExecuteChanged.
   CanExecute must be simple lambda of a property with property change notification:

   public ICommand TestCommand => new AutoDelegateCommand(() => Test(), () => CanExecuteTest);

   private bool canExecuteTest;
   public bool CanExecuteTest
   {
     get => canExecuteTest;
     set
     {
       canExecuteTest = value;
       OnPropertyChanged();
     }
   }
  */
  public class AutoDelegateCommand : ICommand
  {
    private readonly Func<bool> canExecute;
    private readonly Action execute;
    private readonly string memberName;

    public event EventHandler CanExecuteChanged;

    public AutoDelegateCommand(Action execute, Expression<Func<bool>> canExecute = null)
    {
      this.execute = execute;
      this.canExecute = canExecute == null
        ? () => true 
        : canExecute.Compile();

      if (!IsProperMember(canExecute)) return;

      memberName = GetMemberName(canExecute);
      var parentVm = GetParentVm(canExecute);

      if (parentVm != null)
        parentVm.PropertyChanged += ParentClass_PropertyChanged;
    }

    public bool CanExecute(object parameter)
    {
      return canExecute();
    }

    public void Execute(object parameter)
    {
      execute();
    }
    
    protected void OnCanExecuteChanged()
    {
      CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ParentClass_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == memberName) OnCanExecuteChanged();
    }

    private static bool IsProperMember(Expression<Func<bool>> canExecute)
    {
      var memberExpression = canExecute?.Body as MemberExpression;
      return memberExpression?.Type == typeof(bool);
    }

    private static string GetMemberName(Expression<Func<bool>> canExecute)
    {
      var memberExpression = canExecute.Body as MemberExpression;
      return memberExpression?.Member.Name;
    }

    private static INotifyPropertyChanged GetParentVm(Expression<Func<bool>> canExecute)
    {
      var memberExpression = canExecute.Body as MemberExpression;
      var parentClassExpression = memberExpression?.Expression as ConstantExpression;
      return parentClassExpression?.Value as INotifyPropertyChanged;
    }
  }
}
