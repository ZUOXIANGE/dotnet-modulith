using DotNetModulith.Modules.Books.Domain;
using DotNetModulith.Modules.Books.Infrastructure;
using DotNetModulith.Modules.Members.Domain;
using DotNetModulith.Modules.Members.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Api.Data;

internal interface ILibraryDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}

internal sealed class LibraryDataSeeder : ILibraryDataSeeder
{
    private readonly BooksDbContext _booksDb;
    private readonly MembersDbContext _membersDb;

    public LibraryDataSeeder(BooksDbContext booksDb, MembersDbContext membersDb)
    {
        _booksDb = booksDb;
        _membersDb = membersDb;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedCategoriesAsync(cancellationToken);
        await SeedBooksAsync(cancellationToken);
        await SeedMembersAsync(cancellationToken);
    }

    private async Task SeedCategoriesAsync(CancellationToken ct)
    {
        if (await _booksDb.Categories.AnyAsync(ct))
            return;

        var now = DateTimeOffset.UtcNow;

        var computerScience = CategoryEntity.Create("计算机科学", "计算机编程、人工智能、数据科学等相关图书", null, 1, now);
        var literature = CategoryEntity.Create("文学", "小说、诗歌、散文、戏剧等文学类图书", null, 2, now);
        var history = CategoryEntity.Create("历史", "中国历史、世界历史、历史人物传记等", null, 3, now);
        var philosophy = CategoryEntity.Create("哲学", "哲学理论、伦理学、逻辑学等哲学图书", null, 4, now);
        var economics = CategoryEntity.Create("经济管理", "经济学、管理学、市场营销、财务会计等", null, 5, now);
        var naturalScience = CategoryEntity.Create("自然科学", "数学、物理、化学、生物等自然科学图书", null, 6, now);
        var art = CategoryEntity.Create("艺术设计", "绘画、书法、音乐、摄影、设计等艺术类图书", null, 7, now);
        var socialScience = CategoryEntity.Create("社会科学", "社会学、法学、政治学、教育学等", null, 8, now);
        var medicine = CategoryEntity.Create("医学健康", "临床医学、药学、中医学、健康养生等", null, 9, now);
        var language = CategoryEntity.Create("语言学习", "英语、日语、韩语、法语等外语学习图书", null, 10, now);

        _booksDb.Categories.AddRange(computerScience, literature, history, philosophy, economics,
            naturalScience, art, socialScience, medicine, language);

        await _booksDb.SaveChangesAsync(ct);
    }

    private async Task SeedBooksAsync(CancellationToken ct)
    {
        if (await _booksDb.Books.AnyAsync(ct))
            return;

        var now = DateTimeOffset.UtcNow;

        var categories = await _booksDb.Categories.ToListAsync(ct);
        var categoryMap = categories.ToDictionary(c => c.Name, c => c.Id);

        var books = new List<BookEntity>
        {
            BookEntity.Create("978-7-302-23746-4", "数据结构与算法分析", "Mark Allen Weiss", "清华大学出版社", new DateOnly(2010, 3, 1), "经典数据结构与算法教材，涵盖栈、队列、树、图等核心数据结构和排序、搜索等算法", categoryMap["计算机科学"], 5, string.Empty, now),
            BookEntity.Create("978-7-111-40701-0", "深入理解计算机系统", "Randal E. Bryant", "机械工业出版社", new DateOnly(2016, 11, 1), "从程序员视角深入理解计算机系统的经典著作，涵盖内存、处理器、编译器、操作系统等主题", categoryMap["计算机科学"], 3, string.Empty, now),
            BookEntity.Create("978-7-115-51784-2", "Python编程：从入门到实践", "Eric Matthes", "人民邮电出版社", new DateOnly(2020, 8, 1), "零基础学Python的最佳入门书，包含大量实战项目练习", categoryMap["计算机科学"], 6, string.Empty, now),
            BookEntity.Create("978-7-302-55270-5", "人工智能：一种现代方法", "Stuart Russell", "清华大学出版社", new DateOnly(2020, 1, 1), "人工智能领域的经典教材，全面覆盖从搜索算法到深度学习的AI知识体系", categoryMap["计算机科学"], 2, string.Empty, now),
            BookEntity.Create("978-7-5086-8337-7", "原则", "Ray Dalio", "中信出版社", new DateOnly(2018, 1, 1), "全球顶级投资家分享的生活和工作原则，帮助建立系统性决策框架", categoryMap["经济管理"], 4, string.Empty, now),
            BookEntity.Create("978-7-5217-4712-8", "思考，快与慢", "Daniel Kahneman", "中信出版社", new DateOnly(2012, 7, 1), "诺贝尔经济学奖得主卡尼曼的经典作品，揭示人类决策中的认知偏差", categoryMap["经济管理"], 3, string.Empty, now),
            BookEntity.Create("978-7-111-55936-2", "从零到一", "Peter Thiel", "中信出版社", new DateOnly(2015, 1, 1), "硅谷创投教父分享创业与创新的核心思维，如何在竞争中建立垄断优势", categoryMap["经济管理"], 4, string.Empty, now),
            BookEntity.Create("978-7-02-010493-8", "活着", "余华", "人民文学出版社", new DateOnly(2017, 6, 1), "讲述一个普通人在中国历史巨变中的悲欢离合，被誉为中国当代文学经典", categoryMap["文学"], 5, string.Empty, now),
            BookEntity.Create("978-7-5442-5399-8", "百年孤独", "Gabriel García Márquez", "南海出版公司", new DateOnly(2011, 6, 1), "魔幻现实主义文学的代表作，讲述布恩迪亚家族七代人的传奇故事", categoryMap["文学"], 3, string.Empty, now),
            BookEntity.Create("978-7-5321-4628-2", "三体", "刘慈欣", "重庆出版社", new DateOnly(2008, 1, 1), "中国科幻文学的里程碑，讲述了地球文明与外星文明首次接触的故事", categoryMap["文学"], 5, string.Empty, now),
            BookEntity.Create("978-7-5321-4629-9", "三体II：黑暗森林", "刘慈欣", "重庆出版社", new DateOnly(2008, 5, 1), "三体系列第二部，提出宇宙社会学与黑暗森林法则", categoryMap["文学"], 4, string.Empty, now),
            BookEntity.Create("978-7-5321-4630-5", "三体III：死神永生", "刘慈欣", "重庆出版社", new DateOnly(2010, 11, 1), "三体系列终章，完成了对人类命运与宇宙终极图景的恢弘想象", categoryMap["文学"], 4, string.Empty, now),
            BookEntity.Create("978-7-108-06818-3", "万历十五年", "黄仁宇", "生活·读书·新知三联书店", new DateOnly(2019, 3, 1), "以万历十五年为切口，透视明朝政治制度的深层运作逻辑", categoryMap["历史"], 3, string.Empty, now),
            BookEntity.Create("978-7-5633-9285-8", "人类简史", "Yuval Noah Harari", "中信出版社", new DateOnly(2014, 11, 1), "从认知革命到人工智能，用宏观视角回顾人类文明发展历程", categoryMap["历史"], 4, string.Empty, now),
            BookEntity.Create("978-7-5086-4626-9", "枪炮、病菌与钢铁", "Jared Diamond", "中信出版社", new DateOnly(2016, 7, 1), "从地理与环境角度解释各大陆文明发展差异的普利策奖获奖作品", categoryMap["历史"], 2, string.Empty, now),
            BookEntity.Create("978-7-100-11952-3", "中国哲学简史", "冯友兰", "商务印书馆", new DateOnly(2015, 1, 1), "系统梳理从先秦到近代的中国哲学发展脉络，是了解中国哲学的入门经典", categoryMap["哲学"], 3, string.Empty, now),
            BookEntity.Create("978-7-108-05781-9", "西方哲学史", "Bertrand Russell", "生活·读书·新知三联书店", new DateOnly(2016, 1, 1), "诺贝尔文学奖得主罗素所著，以清晰文笔梳理从古希腊到近代西方哲学", categoryMap["哲学"], 2, string.Empty, now),
            BookEntity.Create("978-7-100-12145-7", "纯粹理性批判", "Immanuel Kant", "商务印书馆", new DateOnly(2017, 8, 1), "康德三大批判之首，探讨人类理性的边界和先天知识的可能性", categoryMap["哲学"], 2, string.Empty, now),
            BookEntity.Create("978-7-115-49054-9", "微积分", "James Stewart", "人民邮电出版社", new DateOnly(2018, 10, 1), "全球最畅销的微积分教材，以直观易懂的方式呈现微积分核心概念", categoryMap["自然科学"], 3, string.Empty, now),
            BookEntity.Create("978-7-04-046122-2", "大学物理", "张三慧", "高等教育出版社", new DateOnly(2016, 1, 1), "面向理工科本科生的经典物理教材，涵盖力学、热学、电磁学、光学和近代物理", categoryMap["自然科学"], 4, string.Empty, now),
            BookEntity.Create("978-7-03-058290-4", "分子生物学", "James D. Watson", "科学出版社", new DateOnly(2019, 3, 1), "诺贝尔奖得主Watson编著，分子生物学的经典权威教材", categoryMap["自然科学"], 2, string.Empty, now),
            BookEntity.Create("978-7-5356-3689-3", "艺术的故事", "E.H. Gombrich", "广西美术出版社", new DateOnly(2014, 4, 1), "最畅销的艺术史入门读物，从史前洞窟壁画到现代实验艺术", categoryMap["艺术设计"], 2, string.Empty, now),
            BookEntity.Create("978-7-5153-4344-2", "写给大家看的设计书", "Robin Williams", "人民邮电出版社", new DateOnly(2016, 1, 1), "用最简明的语言讲解四大设计原则：亲密性、对齐、重复和对比", categoryMap["艺术设计"], 3, string.Empty, now),
            BookEntity.Create("978-7-5502-1783-6", "美国纽约摄影学院摄影教材", "美国纽约摄影学院", "北京联合出版公司", new DateOnly(2015, 8, 1), "摄影爱好者的圣经，系统讲解从相机操作到构图用光的完整知识", categoryMap["艺术设计"], 2, string.Empty, now),
            BookEntity.Create("978-7-301-28934-5", "社会心理学", "David G. Myers", "北京大学出版社", new DateOnly(2018, 7, 1), "全球最广泛使用的社会心理学教材，探讨社会思维、社会影响与社会关系", categoryMap["社会科学"], 3, string.Empty, now),
            BookEntity.Create("978-7-5118-2999-4", "论法的精神", "Montesquieu", "法律出版社", new DateOnly(2012, 5, 1), "法学经典著作，提出三权分立思想，深刻影响现代民主制度设计", categoryMap["社会科学"], 2, string.Empty, now),
            BookEntity.Create("978-7-5609-6617-2", "教育学基础", "全国十二所重点师范大学", "教育科学出版社", new DateOnly(2013, 8, 1), "师范院校教育学入门教材，系统阐述教育基本理论和实践问题", categoryMap["社会科学"], 3, string.Empty, now),
            BookEntity.Create("978-7-117-21178-1", "实用内科学", "陈灏珠", "人民卫生出版社", new DateOnly(2015, 10, 1), "临床内科经典参考书，涵盖内科各系统疾病的诊断与治疗", categoryMap["医学健康"], 2, string.Empty, now),
            BookEntity.Create("978-7-5214-1847-9", "求医不如求己", "中里巴人", "中国中医药出版社", new DateOnly(2019, 1, 1), "通俗易懂的中医养生科普读物，介绍经络穴位与日常保健方法", categoryMap["医学健康"], 3, string.Empty, now),
            BookEntity.Create("978-7-5135-9030-8", "新概念英语1", "L.G. Alexander", "外语教学与研究出版社", new DateOnly(2017, 6, 1), "经典的英语学习教材，以情景对话为基础培养英语语感", categoryMap["语言学习"], 5, string.Empty, now),
            BookEntity.Create("978-7-5135-9031-5", "新概念英语2", "L.G. Alexander", "外语教学与研究出版社", new DateOnly(2017, 6, 1), "新概念英语第二册，通过短文训练提升阅读和语法能力", categoryMap["语言学习"], 5, string.Empty, now),
            BookEntity.Create("978-7-100-11115-8", "牛津高阶英汉双解词典", "A.S. Hornby", "商务印书馆", new DateOnly(2018, 3, 1), "英语学习者的必备工具书，收录超过18万单词和短语", categoryMap["语言学习"], 2, string.Empty, now),
            BookEntity.Create("978-7-115-55025-0", "深度学习", "Ian Goodfellow", "人民邮电出版社", new DateOnly(2020, 7, 1), "深度学习领域的奠基性教材，全面覆盖从基础数学到前沿模型", categoryMap["计算机科学"], 2, string.Empty, now),
            BookEntity.Create("978-7-111-64331-8", "设计模式：可复用面向对象软件的基础", "Erich Gamma", "机械工业出版社", new DateOnly(2019, 5, 1), "面向对象设计的经典之作，系统阐述23种设计模式", categoryMap["计算机科学"], 3, string.Empty, now),
            BookEntity.Create("978-7-302-52486-1", "计算机网络：自顶向下方法", "James F. Kurose", "清华大学出版社", new DateOnly(2018, 12, 1), "计算机网络经典教材，以应用层为起点的自顶向下教学方式", categoryMap["计算机科学"], 3, string.Empty, now),
            BookEntity.Create("978-7-111-57172-2", "MySQL技术内幕", "姜承尧", "机械工业出版社", new DateOnly(2017, 6, 1), "深入剖析MySQL存储引擎、索引、事务和SQL优化的核心技术", categoryMap["计算机科学"], 3, string.Empty, now),
            BookEntity.Create("978-7-115-48323-4", "JavaScript高级程序设计", "Matt Frisbie", "人民邮电出版社", new DateOnly(2018, 10, 1), "JavaScript开发者必读的红宝书，深入讲解语言核心和DOM编程", categoryMap["计算机科学"], 4, string.Empty, now),
            BookEntity.Create("978-7-02-011211-0", "红楼梦", "曹雪芹", "人民文学出版社", new DateOnly(2017, 3, 1), "中国古典四大名著之首，以贾宝玉和林黛玉爱情悲剧为主线展现封建家族兴衰", categoryMap["文学"], 5, string.Empty, now),
            BookEntity.Create("978-7-02-011212-7", "围城", "钱锺书", "人民文学出版社", new DateOnly(2017, 4, 1), "描绘知识分子的困境与幽默，被誉为中国现代文学最优秀的长篇小说之一", categoryMap["文学"], 4, string.Empty, now),
            BookEntity.Create("978-7-5321-5949-6", "平凡的世界", "路遥", "北京十月文艺出版社", new DateOnly(2017, 6, 1), "全景式展现中国当代城乡社会生活的长篇小说，茅盾文学奖获奖作品", categoryMap["文学"], 4, string.Empty, now),
        };

        _booksDb.Books.AddRange(books);
        await _booksDb.SaveChangesAsync(ct);
    }

    private async Task SeedMembersAsync(CancellationToken ct)
    {
        if (await _membersDb.Members.AnyAsync(ct))
            return;

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        var members = new List<MemberEntity>
        {
            MemberEntity.Create("张伟", "13800138001", "zhangwei@email.com", "北京市海淀区中关村大街1号", MembershipType.Vip, today, today.AddYears(2), now),
            MemberEntity.Create("李娜", "13800138002", "lina@email.com", "北京市朝阳区望京西路10号", MembershipType.Teacher, today, today.AddYears(1), now),
            MemberEntity.Create("王芳", "13800138003", "wangfang@email.com", "北京市西城区金融街5号", MembershipType.Student, today, today.AddYears(1), now),
            MemberEntity.Create("陈明", "13800138004", "chenming@email.com", "上海市浦东新区陆家嘴环路100号", MembershipType.Teacher, today, today.AddYears(1), now),
            MemberEntity.Create("赵静", "13800138005", "zhaojing@email.com", "上海市徐汇区衡山路2号", MembershipType.Student, today, today.AddYears(1), now),
            MemberEntity.Create("杨涛", "13800138006", "yangtao@email.com", "广州市天河区天河路385号", MembershipType.Vip, today, today.AddYears(2), now),
            MemberEntity.Create("刘洋", "13800138007", "liuyang@email.com", "广州市越秀区北京路100号", MembershipType.Student, today, today.AddYears(1), now),
            MemberEntity.Create("黄丽", "13800138008", "huangli@email.com", "深圳市南山区科技园南路1号", MembershipType.Teacher, today, today.AddYears(1), now),
            MemberEntity.Create("周杰", "13800138009", "zhoujie@email.com", "深圳市福田区华强北路100号", MembershipType.Normal, today, today.AddYears(1), now),
            MemberEntity.Create("吴敏", "13800138010", "wumin@email.com", "杭州市西湖区文三路500号", MembershipType.Student, today, today.AddYears(1), now),
            MemberEntity.Create("孙磊", "13800138011", "sunlei@email.com", "杭州市滨江区江南大道99号", MembershipType.Teacher, today, today.AddYears(1), now),
            MemberEntity.Create("钱峰", "13800138012", "qianfeng@email.com", "南京市鼓楼区中山路100号", MembershipType.Vip, today, today.AddYears(2), now),
            MemberEntity.Create("马晓燕", "13800138013", "maxiaoyan@email.com", "武汉市武昌区珞喻路1037号", MembershipType.Student, today, today.AddYears(1), now),
            MemberEntity.Create("曹宇", "13800138014", "caoyu@email.com", "成都市武侯区一环路南一段24号", MembershipType.Teacher, today, today.AddYears(1), now),
            MemberEntity.Create("蒋雪", "13800138015", "jiangxue@email.com", "成都市锦江区春熙路100号", MembershipType.Student, today, today.AddYears(1), now),
            MemberEntity.Create("冯鹏", "13800138016", "fengpeng@email.com", "西安市雁塔区科技路1号", MembershipType.Normal, today, today.AddYears(1), now),
            MemberEntity.Create("何雨", "13800138017", "heyun@email.com", "长沙市岳麓区麓山南路932号", MembershipType.Student, today, today.AddYears(1), now),
            MemberEntity.Create("董超", "13800138018", "dongchao@email.com", "济南市历城区山大南路27号", MembershipType.Teacher, today, today.AddYears(1), now),
            MemberEntity.Create("袁文", "13800138019", "yuanwen@email.com", "重庆市渝中区解放碑步行街1号", MembershipType.Vip, today, today.AddYears(2), now),
            MemberEntity.Create("徐晨", "13800138020", "xuchen@email.com", "天津市南开区卫津路94号", MembershipType.Student, today, today.AddYears(1), now),
        };

        _membersDb.Members.AddRange(members);
        await _membersDb.SaveChangesAsync(ct);
    }
}
