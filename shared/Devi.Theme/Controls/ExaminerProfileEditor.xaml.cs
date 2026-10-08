using System.Windows.Controls;
using Devi.Theme.Profile;

namespace Devi.Theme.Controls;

/// <summary>Edits the shared <see cref="ExaminerProfile"/>. The host dialog decides when to save.</summary>
public partial class ExaminerProfileEditor : UserControl
{
    public ExaminerProfileEditor()
    {
        InitializeComponent();
    }

    public void Show(ExaminerProfile profile)
    {
        ExaminerNameBox.Text = profile.ExaminerName ?? "";
        AgencyBox.Text = profile.Agency ?? "";
        UnitBox.Text = profile.Unit ?? "";
        TitleBox.Text = profile.TitleOrBadge ?? "";
        ContactBox.Text = profile.Contact ?? "";
    }

    public ExaminerProfile Read() => new ExaminerProfile
    {
        ExaminerName = ExaminerNameBox.Text,
        Agency = AgencyBox.Text,
        Unit = UnitBox.Text,
        TitleOrBadge = TitleBox.Text,
        Contact = ContactBox.Text,
    };

    public void FocusFirst() => ExaminerNameBox.Focus();
}
