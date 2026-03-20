namespace ExamSystem.Authorization;

/// <summary>
/// Danh sách permission codes — phải khớp với dữ liệu seed trong DbSeeder
/// </summary>
public static class Permissions
{
    public const string UserView   = "USER_VIEW";
    public const string UserCreate = "USER_CREATE";
    public const string UserUpdate = "USER_UPDATE";
    public const string UserDelete = "USER_DELETE";

    public const string RoleView             = "ROLE_VIEW";
    public const string RoleCreate           = "ROLE_CREATE";
    public const string RoleUpdate           = "ROLE_UPDATE";
    public const string RoleDelete           = "ROLE_DELETE";
    public const string RoleAssignPermission = "ROLE_ASSIGN_PERMISSION";

    public const string PermissionView   = "PERMISSION_VIEW";
    public const string PermissionCreate = "PERMISSION_CREATE";
    public const string PermissionUpdate = "PERMISSION_UPDATE";
    public const string PermissionDelete = "PERMISSION_DELETE";

    public const string SubjectView   = "SUBJECT_VIEW";
    public const string SubjectCreate = "SUBJECT_CREATE";
    public const string SubjectUpdate = "SUBJECT_UPDATE";
    public const string SubjectDelete = "SUBJECT_DELETE";

    public const string QuestionView   = "QUESTION_VIEW";
    public const string QuestionCreate = "QUESTION_CREATE";
    public const string QuestionUpdate = "QUESTION_UPDATE";
    public const string QuestionDelete = "QUESTION_DELETE";

    public const string ExamView    = "EXAM_VIEW";
    public const string ExamCreate  = "EXAM_CREATE";
    public const string ExamUpdate  = "EXAM_UPDATE";
    public const string ExamDelete  = "EXAM_DELETE";
    public const string ExamAssign  = "EXAM_ASSIGN";
    public const string ExamPublish = "EXAM_PUBLISH";

    public const string GroupView         = "GROUP_VIEW";
    public const string GroupCreate       = "GROUP_CREATE";
    public const string GroupUpdate       = "GROUP_UPDATE";
    public const string GroupDelete       = "GROUP_DELETE";
    public const string GroupMemberManage = "GROUP_MEMBER_MANAGE";

    public const string ExamSessionView  = "EXAM_SESSION_VIEW";
    public const string ExamSessionGrade = "EXAM_SESSION_GRADE";
}
