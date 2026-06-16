<template>
  <div class="page-container">
    <div class="page-header">工作台</div>
    <n-grid :cols="4" :x-gap="16" :y-gap="16">
      <n-grid-item>
        <n-card title="图书总量" size="small">
          <n-statistic tabular-nums>
            <n-number-animation :from="0" :to="12850" />
          </n-statistic>
        </n-card>
      </n-grid-item>
      <n-grid-item>
        <n-card title="读者总数" size="small">
          <n-statistic tabular-nums>
            <n-number-animation :from="0" :to="3256" />
          </n-statistic>
        </n-card>
      </n-grid-item>
      <n-grid-item>
        <n-card title="在借图书" size="small">
          <n-statistic tabular-nums>
            <n-number-animation :from="0" :to="423" />
          </n-statistic>
        </n-card>
      </n-grid-item>
      <n-grid-item>
        <n-card title="逾期未还" size="small">
          <n-statistic tabular-nums>
            <n-number-animation :from="0" :to="18" />
          </n-statistic>
        </n-card>
      </n-grid-item>
    </n-grid>
    <n-card title="近期借阅动态" class="mt-16">
      <n-empty description="数据加载中..." />
    </n-card>
  </div>
</template>

<script setup lang="ts">
</script>

<style scoped>
.mt-16 {
  margin-top: 16px;
}
</style>