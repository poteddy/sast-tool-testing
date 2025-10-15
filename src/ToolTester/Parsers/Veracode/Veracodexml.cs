using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolTester.Parsers.Veracode
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "https://www.veracode.com/schema/reports/export/1.0", IsNullable = false)]
    public partial class detailedreport
    {

        private detailedreportStaticanalysis staticanalysisField;

        private detailedreportSeverity[] severityField;

        private detailedreportFlawstatus flawstatusField;

        private detailedreportCustomfield[] customfieldsField;

        private detailedreportSoftware_composition_analysis software_composition_analysisField;

        private decimal report_format_versionField;

        private ushort account_idField;

        private string app_nameField;

        private uint app_idField;

        private uint analysis_idField;

        private uint static_analysis_unit_idField;

        private uint sandbox_idField;

        private string first_build_submitted_dateField;

        private string versionField;

        private uint build_idField;

        private string submitterField;

        private string platformField;

        private byte assurance_levelField;

        private byte business_criticalityField;

        private string generation_dateField;

        private string veracode_levelField;

        private ushort total_flawsField;

        private ushort flaws_not_mitigatedField;

        private string teamsField;

        private string life_cycle_stageField;

        private string planned_deployment_dateField;

        private string last_update_timeField;

        private bool is_latest_buildField;

        private string policy_nameField;

        private byte policy_versionField;

        private string policy_compliance_statusField;

        private string policy_rules_statusField;

        private bool grace_period_expiredField;

        private bool scan_overdueField;

        private string business_ownerField;

        private string business_unitField;

        private string tagsField;

        private bool legacy_scan_engineField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("static-analysis")]
        public detailedreportStaticanalysis staticanalysis
        {
            get
            {
                return this.staticanalysisField;
            }
            set
            {
                this.staticanalysisField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("severity")]
        public detailedreportSeverity[] severity
        {
            get
            {
                return this.severityField;
            }
            set
            {
                this.severityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("flaw-status")]
        public detailedreportFlawstatus flawstatus
        {
            get
            {
                return this.flawstatusField;
            }
            set
            {
                this.flawstatusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("customfield", IsNullable = false)]
        public detailedreportCustomfield[] customfields
        {
            get
            {
                return this.customfieldsField;
            }
            set
            {
                this.customfieldsField = value;
            }
        }

        /// <remarks/>
        public detailedreportSoftware_composition_analysis software_composition_analysis
        {
            get
            {
                return this.software_composition_analysisField;
            }
            set
            {
                this.software_composition_analysisField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal report_format_version
        {
            get
            {
                return this.report_format_versionField;
            }
            set
            {
                this.report_format_versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort account_id
        {
            get
            {
                return this.account_idField;
            }
            set
            {
                this.account_idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string app_name
        {
            get
            {
                return this.app_nameField;
            }
            set
            {
                this.app_nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint app_id
        {
            get
            {
                return this.app_idField;
            }
            set
            {
                this.app_idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint analysis_id
        {
            get
            {
                return this.analysis_idField;
            }
            set
            {
                this.analysis_idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint static_analysis_unit_id
        {
            get
            {
                return this.static_analysis_unit_idField;
            }
            set
            {
                this.static_analysis_unit_idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint sandbox_id
        {
            get
            {
                return this.sandbox_idField;
            }
            set
            {
                this.sandbox_idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string first_build_submitted_date
        {
            get
            {
                return this.first_build_submitted_dateField;
            }
            set
            {
                this.first_build_submitted_dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint build_id
        {
            get
            {
                return this.build_idField;
            }
            set
            {
                this.build_idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string submitter
        {
            get
            {
                return this.submitterField;
            }
            set
            {
                this.submitterField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string platform
        {
            get
            {
                return this.platformField;
            }
            set
            {
                this.platformField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte assurance_level
        {
            get
            {
                return this.assurance_levelField;
            }
            set
            {
                this.assurance_levelField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte business_criticality
        {
            get
            {
                return this.business_criticalityField;
            }
            set
            {
                this.business_criticalityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string generation_date
        {
            get
            {
                return this.generation_dateField;
            }
            set
            {
                this.generation_dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string veracode_level
        {
            get
            {
                return this.veracode_levelField;
            }
            set
            {
                this.veracode_levelField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort total_flaws
        {
            get
            {
                return this.total_flawsField;
            }
            set
            {
                this.total_flawsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort flaws_not_mitigated
        {
            get
            {
                return this.flaws_not_mitigatedField;
            }
            set
            {
                this.flaws_not_mitigatedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string teams
        {
            get
            {
                return this.teamsField;
            }
            set
            {
                this.teamsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string life_cycle_stage
        {
            get
            {
                return this.life_cycle_stageField;
            }
            set
            {
                this.life_cycle_stageField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string planned_deployment_date
        {
            get
            {
                return this.planned_deployment_dateField;
            }
            set
            {
                this.planned_deployment_dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string last_update_time
        {
            get
            {
                return this.last_update_timeField;
            }
            set
            {
                this.last_update_timeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool is_latest_build
        {
            get
            {
                return this.is_latest_buildField;
            }
            set
            {
                this.is_latest_buildField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string policy_name
        {
            get
            {
                return this.policy_nameField;
            }
            set
            {
                this.policy_nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte policy_version
        {
            get
            {
                return this.policy_versionField;
            }
            set
            {
                this.policy_versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string policy_compliance_status
        {
            get
            {
                return this.policy_compliance_statusField;
            }
            set
            {
                this.policy_compliance_statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string policy_rules_status
        {
            get
            {
                return this.policy_rules_statusField;
            }
            set
            {
                this.policy_rules_statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool grace_period_expired
        {
            get
            {
                return this.grace_period_expiredField;
            }
            set
            {
                this.grace_period_expiredField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool scan_overdue
        {
            get
            {
                return this.scan_overdueField;
            }
            set
            {
                this.scan_overdueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string business_owner
        {
            get
            {
                return this.business_ownerField;
            }
            set
            {
                this.business_ownerField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string business_unit
        {
            get
            {
                return this.business_unitField;
            }
            set
            {
                this.business_unitField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string tags
        {
            get
            {
                return this.tagsField;
            }
            set
            {
                this.tagsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool legacy_scan_engine
        {
            get
            {
                return this.legacy_scan_engineField;
            }
            set
            {
                this.legacy_scan_engineField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportStaticanalysis
    {

        private detailedreportStaticanalysisModule[] modulesField;

        private string ratingField;

        private byte scoreField;

        private string submitted_dateField;

        private string published_dateField;

        private string versionField;

        private uint analysis_size_bytesField;

        private ulong engine_versionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("module", IsNullable = false)]
        public detailedreportStaticanalysisModule[] modules
        {
            get
            {
                return this.modulesField;
            }
            set
            {
                this.modulesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string rating
        {
            get
            {
                return this.ratingField;
            }
            set
            {
                this.ratingField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte score
        {
            get
            {
                return this.scoreField;
            }
            set
            {
                this.scoreField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string submitted_date
        {
            get
            {
                return this.submitted_dateField;
            }
            set
            {
                this.submitted_dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string published_date
        {
            get
            {
                return this.published_dateField;
            }
            set
            {
                this.published_dateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint analysis_size_bytes
        {
            get
            {
                return this.analysis_size_bytesField;
            }
            set
            {
                this.analysis_size_bytesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ulong engine_version
        {
            get
            {
                return this.engine_versionField;
            }
            set
            {
                this.engine_versionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportStaticanalysisModule
    {

        private string nameField;

        private string compilerField;

        private string osField;

        private string architectureField;

        private uint locField;

        private byte scoreField;

        private byte numflawssev0Field;

        private byte numflawssev1Field;

        private ushort numflawssev2Field;

        private ushort numflawssev3Field;

        private byte numflawssev4Field;

        private ushort numflawssev5Field;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string compiler
        {
            get
            {
                return this.compilerField;
            }
            set
            {
                this.compilerField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string os
        {
            get
            {
                return this.osField;
            }
            set
            {
                this.osField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string architecture
        {
            get
            {
                return this.architectureField;
            }
            set
            {
                this.architectureField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint loc
        {
            get
            {
                return this.locField;
            }
            set
            {
                this.locField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte score
        {
            get
            {
                return this.scoreField;
            }
            set
            {
                this.scoreField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte numflawssev0
        {
            get
            {
                return this.numflawssev0Field;
            }
            set
            {
                this.numflawssev0Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte numflawssev1
        {
            get
            {
                return this.numflawssev1Field;
            }
            set
            {
                this.numflawssev1Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort numflawssev2
        {
            get
            {
                return this.numflawssev2Field;
            }
            set
            {
                this.numflawssev2Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort numflawssev3
        {
            get
            {
                return this.numflawssev3Field;
            }
            set
            {
                this.numflawssev3Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte numflawssev4
        {
            get
            {
                return this.numflawssev4Field;
            }
            set
            {
                this.numflawssev4Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort numflawssev5
        {
            get
            {
                return this.numflawssev5Field;
            }
            set
            {
                this.numflawssev5Field = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverity
    {

        private detailedreportSeverityCategory[] categoryField;

        private byte levelField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("category")]
        public detailedreportSeverityCategory[] category
        {
            get
            {
                return this.categoryField;
            }
            set
            {
                this.categoryField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte level
        {
            get
            {
                return this.levelField;
            }
            set
            {
                this.levelField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategory
    {

        private detailedreportSeverityCategoryPara[] descField;

        private detailedreportSeverityCategoryPara1[] recommendationsField;

        private detailedreportSeverityCategoryCwe[] cweField;

        private byte categoryidField;

        private string categorynameField;

        private bool pcirelatedField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("para", IsNullable = false)]
        public detailedreportSeverityCategoryPara[] desc
        {
            get
            {
                return this.descField;
            }
            set
            {
                this.descField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("para", IsNullable = false)]
        public detailedreportSeverityCategoryPara1[] recommendations
        {
            get
            {
                return this.recommendationsField;
            }
            set
            {
                this.recommendationsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("cwe")]
        public detailedreportSeverityCategoryCwe[] cwe
        {
            get
            {
                return this.cweField;
            }
            set
            {
                this.cweField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte categoryid
        {
            get
            {
                return this.categoryidField;
            }
            set
            {
                this.categoryidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string categoryname
        {
            get
            {
                return this.categorynameField;
            }
            set
            {
                this.categorynameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool pcirelated
        {
            get
            {
                return this.pcirelatedField;
            }
            set
            {
                this.pcirelatedField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryPara
    {

        private detailedreportSeverityCategoryParaBulletitem[] bulletitemField;

        private string textField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("bulletitem")]
        public detailedreportSeverityCategoryParaBulletitem[] bulletitem
        {
            get
            {
                return this.bulletitemField;
            }
            set
            {
                this.bulletitemField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryParaBulletitem
    {

        private string textField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryPara1
    {

        private detailedreportSeverityCategoryParaBulletitem1[] bulletitemField;

        private string textField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("bulletitem")]
        public detailedreportSeverityCategoryParaBulletitem1[] bulletitem
        {
            get
            {
                return this.bulletitemField;
            }
            set
            {
                this.bulletitemField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryParaBulletitem1
    {

        private string textField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryCwe
    {

        private detailedreportSeverityCategoryCweDescription descriptionField;

        private detailedreportSeverityCategoryCweFlaw[] staticflawsField;

        private ushort cweidField;

        private string cwenameField;

        private bool pcirelatedField;

        private ushort sansField;

        private bool sansFieldSpecified;

        private ushort certcField;

        private bool certcFieldSpecified;

        private ushort owaspField;

        private bool owaspFieldSpecified;

        private ushort certcppField;

        private bool certcppFieldSpecified;

        private ushort certjavaField;

        private bool certjavaFieldSpecified;

        /// <remarks/>
        public detailedreportSeverityCategoryCweDescription description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("flaw", IsNullable = false)]
        public detailedreportSeverityCategoryCweFlaw[] staticflaws
        {
            get
            {
                return this.staticflawsField;
            }
            set
            {
                this.staticflawsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort cweid
        {
            get
            {
                return this.cweidField;
            }
            set
            {
                this.cweidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cwename
        {
            get
            {
                return this.cwenameField;
            }
            set
            {
                this.cwenameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool pcirelated
        {
            get
            {
                return this.pcirelatedField;
            }
            set
            {
                this.pcirelatedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort sans
        {
            get
            {
                return this.sansField;
            }
            set
            {
                this.sansField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool sansSpecified
        {
            get
            {
                return this.sansFieldSpecified;
            }
            set
            {
                this.sansFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort certc
        {
            get
            {
                return this.certcField;
            }
            set
            {
                this.certcField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool certcSpecified
        {
            get
            {
                return this.certcFieldSpecified;
            }
            set
            {
                this.certcFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort owasp
        {
            get
            {
                return this.owaspField;
            }
            set
            {
                this.owaspField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool owaspSpecified
        {
            get
            {
                return this.owaspFieldSpecified;
            }
            set
            {
                this.owaspFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort certcpp
        {
            get
            {
                return this.certcppField;
            }
            set
            {
                this.certcppField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool certcppSpecified
        {
            get
            {
                return this.certcppFieldSpecified;
            }
            set
            {
                this.certcppFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort certjava
        {
            get
            {
                return this.certjavaField;
            }
            set
            {
                this.certjavaField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool certjavaSpecified
        {
            get
            {
                return this.certjavaFieldSpecified;
            }
            set
            {
                this.certjavaFieldSpecified = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryCweDescription
    {

        private detailedreportSeverityCategoryCweDescriptionText textField;

        /// <remarks/>
        public detailedreportSeverityCategoryCweDescriptionText text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryCweDescriptionText
    {

        private string textField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string text
        {
            get
            {
                return this.textField;
            }
            set
            {
                this.textField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryCweFlaw
    {

        private detailedreportSeverityCategoryCweFlawExploitability_adjustments exploitability_adjustmentsField;

        private byte severityField;

        private string categorynameField;

        private byte countField;

        private ushort issueidField;

        private string moduleField;

        private string typeField;

        private string descriptionField;

        private string noteField;

        private ushort cweidField;

        private byte remediationeffortField;

        private sbyte exploitLevelField;

        private byte categoryidField;

        private bool pcirelatedField;

        private string date_first_occurrenceField;

        private string remediation_statusField;

        private string cia_impactField;

        private string grace_period_expiresField;

        private bool affects_policy_complianceField;

        private string mitigation_statusField;

        private string mitigation_status_descField;

        private string sourcefileField;

        private ushort lineField;

        private string sourcefilepathField;

        private string scopeField;

        private string functionprototypeField;

        private byte functionrelativelocationField;

        /// <remarks/>
        public detailedreportSeverityCategoryCweFlawExploitability_adjustments exploitability_adjustments
        {
            get
            {
                return this.exploitability_adjustmentsField;
            }
            set
            {
                this.exploitability_adjustmentsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte severity
        {
            get
            {
                return this.severityField;
            }
            set
            {
                this.severityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string categoryname
        {
            get
            {
                return this.categorynameField;
            }
            set
            {
                this.categorynameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte count
        {
            get
            {
                return this.countField;
            }
            set
            {
                this.countField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort issueid
        {
            get
            {
                return this.issueidField;
            }
            set
            {
                this.issueidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string module
        {
            get
            {
                return this.moduleField;
            }
            set
            {
                this.moduleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string note
        {
            get
            {
                return this.noteField;
            }
            set
            {
                this.noteField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort cweid
        {
            get
            {
                return this.cweidField;
            }
            set
            {
                this.cweidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte remediationeffort
        {
            get
            {
                return this.remediationeffortField;
            }
            set
            {
                this.remediationeffortField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public sbyte exploitLevel
        {
            get
            {
                return this.exploitLevelField;
            }
            set
            {
                this.exploitLevelField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte categoryid
        {
            get
            {
                return this.categoryidField;
            }
            set
            {
                this.categoryidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool pcirelated
        {
            get
            {
                return this.pcirelatedField;
            }
            set
            {
                this.pcirelatedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string date_first_occurrence
        {
            get
            {
                return this.date_first_occurrenceField;
            }
            set
            {
                this.date_first_occurrenceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string remediation_status
        {
            get
            {
                return this.remediation_statusField;
            }
            set
            {
                this.remediation_statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string cia_impact
        {
            get
            {
                return this.cia_impactField;
            }
            set
            {
                this.cia_impactField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string grace_period_expires
        {
            get
            {
                return this.grace_period_expiresField;
            }
            set
            {
                this.grace_period_expiresField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool affects_policy_compliance
        {
            get
            {
                return this.affects_policy_complianceField;
            }
            set
            {
                this.affects_policy_complianceField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string mitigation_status
        {
            get
            {
                return this.mitigation_statusField;
            }
            set
            {
                this.mitigation_statusField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string mitigation_status_desc
        {
            get
            {
                return this.mitigation_status_descField;
            }
            set
            {
                this.mitigation_status_descField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string sourcefile
        {
            get
            {
                return this.sourcefileField;
            }
            set
            {
                this.sourcefileField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort line
        {
            get
            {
                return this.lineField;
            }
            set
            {
                this.lineField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string sourcefilepath
        {
            get
            {
                return this.sourcefilepathField;
            }
            set
            {
                this.sourcefilepathField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string scope
        {
            get
            {
                return this.scopeField;
            }
            set
            {
                this.scopeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string functionprototype
        {
            get
            {
                return this.functionprototypeField;
            }
            set
            {
                this.functionprototypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte functionrelativelocation
        {
            get
            {
                return this.functionrelativelocationField;
            }
            set
            {
                this.functionrelativelocationField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryCweFlawExploitability_adjustments
    {

        private detailedreportSeverityCategoryCweFlawExploitability_adjustmentsExploitability_adjustment exploitability_adjustmentField;

        /// <remarks/>
        public detailedreportSeverityCategoryCweFlawExploitability_adjustmentsExploitability_adjustment exploitability_adjustment
        {
            get
            {
                return this.exploitability_adjustmentField;
            }
            set
            {
                this.exploitability_adjustmentField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSeverityCategoryCweFlawExploitability_adjustmentsExploitability_adjustment
    {

        private string noteField;

        private sbyte score_adjustmentField;

        /// <remarks/>
        public string note
        {
            get
            {
                return this.noteField;
            }
            set
            {
                this.noteField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public sbyte score_adjustment
        {
            get
            {
                return this.score_adjustmentField;
            }
            set
            {
                this.score_adjustmentField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportFlawstatus
    {

        private ushort newField;

        private byte reopenField;

        private byte openField;

        private byte fixedField;

        private ushort totalField;

        private ushort not_mitigatedField;

        private byte sev1changeField;

        private ushort sev2changeField;

        private ushort sev3changeField;

        private byte sev4changeField;

        private ushort sev5changeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort @new
        {
            get
            {
                return this.newField;
            }
            set
            {
                this.newField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte reopen
        {
            get
            {
                return this.reopenField;
            }
            set
            {
                this.reopenField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte open
        {
            get
            {
                return this.openField;
            }
            set
            {
                this.openField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte @fixed
        {
            get
            {
                return this.fixedField;
            }
            set
            {
                this.fixedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort total
        {
            get
            {
                return this.totalField;
            }
            set
            {
                this.totalField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort not_mitigated
        {
            get
            {
                return this.not_mitigatedField;
            }
            set
            {
                this.not_mitigatedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("sev-1-change")]
        public byte sev1change
        {
            get
            {
                return this.sev1changeField;
            }
            set
            {
                this.sev1changeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("sev-2-change")]
        public ushort sev2change
        {
            get
            {
                return this.sev2changeField;
            }
            set
            {
                this.sev2changeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("sev-3-change")]
        public ushort sev3change
        {
            get
            {
                return this.sev3changeField;
            }
            set
            {
                this.sev3changeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("sev-4-change")]
        public byte sev4change
        {
            get
            {
                return this.sev4changeField;
            }
            set
            {
                this.sev4changeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("sev-5-change")]
        public ushort sev5change
        {
            get
            {
                return this.sev5changeField;
            }
            set
            {
                this.sev5changeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportCustomfield
    {

        private string nameField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "https://www.veracode.com/schema/reports/export/1.0")]
    public partial class detailedreportSoftware_composition_analysis
    {

        private object vulnerable_componentsField;

        private byte third_party_componentsField;

        private bool violate_policyField;

        private byte components_violated_policyField;

        /// <remarks/>
        public object vulnerable_components
        {
            get
            {
                return this.vulnerable_componentsField;
            }
            set
            {
                this.vulnerable_componentsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte third_party_components
        {
            get
            {
                return this.third_party_componentsField;
            }
            set
            {
                this.third_party_componentsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool violate_policy
        {
            get
            {
                return this.violate_policyField;
            }
            set
            {
                this.violate_policyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte components_violated_policy
        {
            get
            {
                return this.components_violated_policyField;
            }
            set
            {
                this.components_violated_policyField = value;
            }
        }
    }


}
