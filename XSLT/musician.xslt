<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet 
  version="1.0" 
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:c="http://tempuri.org/schema/catalog#" 
  xmlns:m="http://tempuri.org/schema/music#" 
  xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
  xmlns:mc="http://tempuri.org/schema/music_catalog"
  exclude-result-prefixes="mc"
>
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="/mc:musician">
    <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
      <m:Musician>

        <xsl:variable name="category" select="mc:category" />
        <xsl:variable name="id" select="mc:id" />

        <xsl:attribute name="rdf:about">
          <xsl:value-of select="concat('http://tempuri.org/british/music#', $category, '/', $id)"/>
        </xsl:attribute>

        <c:firstName>
          <xsl:value-of select="mc:name/mc:first"/>
        </c:firstName>
        <c:lastName>
          <xsl:value-of select="mc:name/mc:first"/>
        </c:lastName>
        
        <xsl:for-each select="mc:instrument">
          <m:instrument>
            <xsl:value-of select="."/>
          </m:instrument>
        </xsl:for-each>

        <xsl:for-each select="mc:role">
          <xsl:variable name="role" select="." />
          <rdf:type>
            <xsl:attribute name="rdf:resource">
              <xsl:value-of select="concat('http://tempuri.org/schema/music#', $role)"/>
            </xsl:attribute>
          </rdf:type>
        </xsl:for-each>

      </m:Musician>
    </rdf:RDF>
  </xsl:template>
  
</xsl:stylesheet>
